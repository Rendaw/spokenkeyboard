#!/usr/bin/env python
import re
import json
from subprocess import Popen, PIPE, run
import time
from importlib import import_module, reload
import os.path

import appdirs
from twisted.internet import task
from twisted.internet.defer import Deferred
from twisted.internet.protocol import ClientFactory as BaseClientFactory
from twisted.protocols.basic import LineReceiver
from twisted.internet.task import LoopingCall

from spokenkeyboard_client.tools import top_title
import spokenkeyboard_client.common_tools as common_tools

def_path = appdirs.user_config_dir('spokenkeyboard') + '/definition.py'
if os.path.exists(def_path):
    import importlib.util
    spec = importlib.util.spec_from_file_location('definition', def_path)
    definition = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(definition)
else:
    import spokenkeyboard_client.definition as definition
    def_path = os.path.abspath(definition.__file__)
def_timestamp = os.path.getmtime(def_path)

host = definition.host
port = definition.port

client = None

grammar_lookup = None
rule_lookup = None

def load():
    global grammar_lookup
    global rule_lookup
    rule_lookup = {}
    grammar_lookup = []
    common_tools.push_lookup = {}
    common_tools.current_grammar = None
    common_tools.stack = []

    for gi, grammar in enumerate(definition.grammars):
        name = 'grammar' + str(gi)
        send = {
            'type': 'NewGrammar',
            'name': name,
            'rules': {},
        }
        for ri, rule in enumerate(grammar['rules']):
            rule_name = 'g' + str(gi) + 'r' + str(ri)
            send['rules'][rule_name] = {
                'type': rule['type'],
                'start': rule['start'],
            }
            rule_lookup[rule_name] = rule['action']
        client.sendLine(json.dumps(send).encode('utf-8'))
        if 'condition' in grammar:
            grammar_lookup.append({
                'condition': grammar['condition'],
                'name': name,
            })
        else:
            common_tools.push_lookup[grammar['reference']] = name

class Client(LineReceiver):
    delimiter = b'\n'

    def connectionMade(self):
        global client
        client = self
        def switch_grammar(name):
            if name != common_tools.current_grammar:
                self.sendLine(json.dumps({
                    'type': 'SwitchGrammar',
                    'name': name,
                }).encode('utf-8'))
                common_tools.current_grammar = name
        common_tools.switch_grammar = switch_grammar

        load()

        @LoopingCall
        def sometimes():
            if def_path:
                new_timestamp = os.path.getmtime(def_path)
                global def_timestamp
                if new_timestamp > def_timestamp:
                    def_timestamp = new_timestamp
                    print('definition updated, reloading')
                    reload(definition)
                    load()
 
            if not common_tools.stack:
                title = top_title()
                for grammar in grammar_lookup:
                    if grammar['condition'].search(title):
                        common_tools.switch_grammar(grammar['name'])
                        break
        sometimes.start(1)

    def lineReceived(self, line):
        data = json.loads(line.decode('utf-8'))
        action = rule_lookup[data['name']]
        reset = common_tools.repeat
        for i in range(common_tools.repeat):
            if i != 0:
                time.sleep(0.01)
            if data['type'] == 'VoidResponse':
                action()
            elif data['type'].startswith('Single'):
                action(data['data'])
            else:
                print('unknown rule type', data['name'])
        if reset == common_tools.repeat:
            common_tools.repeat = 1

class ClientFactory(BaseClientFactory):
    protocol = Client

    def __init__(self):
        self.done = Deferred()

    def clientConnectionFailed(self, connector, reason):
        print('connection failed:', reason.getErrorMessage())
        self.done.errback(reason)

    def clientConnectionLost(self, connector, reason):
        print('connection lost:', reason.getErrorMessage())
        self.done.callback(None)

def main():
    @task.react
    def inner(reactor):
        factory = ClientFactory()
        reactor.connectTCP(host, port, factory)
        return factory.done

if __name__ == '__main__':
    main()
