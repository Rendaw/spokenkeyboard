import re
import time

from spokenkeyboard_client.tools import *

host = '192.168.1.198'
port = 21147

vanilla = [
    ('alpha', 'a'),
    ('bravo', 'b'),
    ('charlie', 'c'),
    ('delta', 'd'),
    ('echo', 'e'),
    ('foxtrot', 'f'),
    ('golf', 'g'),
    ('hotel', 'h'),
    ('indigo', 'i'),
    ('juliet', 'j'),
    ('kilo', 'k'),
    ('lima', 'l'),
    ('mike', 'm'),
    ('november', 'n'),
    ('oscar', 'o'),
    ('papa', 'p'),
    ('quiche', 'q'),
    ('romeo', 'r'),
    ('sierra', 's'),
    ('tango', 't'),
    ('uniform', 'u'),
    ('victor', 'v'),
    ('whiskey', 'w'),
    ('x-ray', 'x'),
    ('yankee', 'y'),
    ('zulu', 'z'),
    ('zero', '0'),
    ('one', '1'),
    ('two', '2'),
    ('three', '3'),
    ('four', '4'),
    ('five', '5'),
    ('six', '6'),
    ('seven', '7'),
    ('eight', '8'),
    ('nine', '9'),
    ('left', 'left'),
    ('right', 'right'),
    ('above', 'up'),
    ('down', 'down'),
    ('page up', 'pgup'),
    ('page', 'pgdown'),
    ('page home', 'home'),
    ('page end', 'end'),
    ('space', 'space'),
    ('enter', 'enter'),
    ('return', 'enter'),
    ('escape', 'escape'),
    ('tab', 'tab'),
    ('modify alt', 'alt'),
    ('control', 'ctrl'),
    ('shift', 'shift'),
    ('pipe', '|'),
    ('dash', '-'),
    ('minus', '-'),
    ('hyphen', '-'),
    ('dot', '.'),
    ('period', '.'),
    ('comma', ','),
    ('greater', '>'),
    ('lesser', '<'),
    ('backslash', '\\'),
    ('drop', '\\'),
    ('under', '_'),
    ('star', '*'),
    ('asterisk', '*'),
    ('colon', ':'),
    ('semicolon', ';'),
    ('at', '@'),
    ('double quote', '"'),
    ('quote', '\''),
    ('hash', '#'),
    ('pound', '#'),
    ('dollar', '$'),
    ('percent', '%'),
    ('and', '&'),
    ('ampersand', '&'),
    ('slap', '/'),
    ('equal', '='),
    ('plus', '+'),
    ('space', ' '),
] + [
    ('fink ' + str(x), 'f' + str(x)) for x in range(1, 12)
]

grammars = []

grammars.append({
    'reference': 'sleep',
    'rules': [
        string_rule(start='come', action=lambda value: pop_grammar() if value == 'induction' else None ),
    ],
})

base = [
    void_rule(start=x[0], action=(lambda x: lambda: key(x[1]))(x) ) for x in vanilla
] + [
    void_rule(start='hold ' + x[0], action=(lambda x: lambda: hold(x[1]))(x) ) for x in vanilla
] + [
    void_rule(start='mineralize', action=lambda: push_grammar('sleep') ),
    integer_rule(start='delete back', action=lambda value: [key('backspace') for i in range(value)] ),
    integer_rule(start='delete next', action=lambda value: [key('delete') for i in range(value)] ),
    integer_rule(start='number', action=lambda value: letters(str(value)) ),
    integer_rule(start='snake', action=lambda value: switch_desktop(value) ),
    string_rule(start='garden', action=lambda value: raise_window(value) ),
    integer_rule(start='repeat', action=lambda value: [(action.last(), time.sleep(0.005)) for i in range(value)], norepeat=True),
    void_rule(start='tab next', action=lambda: (hold('control'), key('pgdown')) ),
    void_rule(start='tab back', action=lambda: (hold('control'), key('pgup')) ),
    string_rule(start='caps', action=lambda value: (letters(re.sub('(^| ).', lambda x: x.group()[-1:].upper(), value.lower()))) ),
    string_rule(start='camel', action=lambda value: (letters(re.sub(' .', lambda x: x.group()[-1:].upper(), value.lower()))) ),
    string_rule(start='score', action=lambda value: (letters(re.sub(' ', '_', value.lower()))) ),
    string_rule(start='dashed', action=lambda value: (letters(re.sub(' ', '-', value.lower()))) ),
    string_rule(start='spaced', action=lambda value: (letters(value)) ),
]
terminal_rules = [
    void_rule(start='tab new', action=lambda: (hold('control'), letters('T')) ),
]
vim_rules = [
    void_rule(start='back word', action=lambda: (letters('b')) ),
    void_rule(start='next word', action=lambda: (letters('w')) ),
    void_rule(start='line kill', action=lambda: (key('escape'), letters('dd')) ),
    void_rule(start='line new', action=lambda: (key('escape'), letters('o')) ),
    void_rule(start='line copy', action=lambda: (key('escape'), letters('yy')) ),
    void_rule(start='external paste', action=lambda: (
        key('escape'),
        letters(':set paste'),
        key('enter'),
        letters('i'),
        hold('control'),
        letters('V'),
        key('escape'),
        letters(':set nopaste'),
        key('enter'),
        ) ),
    void_rule(start='save', action=lambda: (key('escape'), letters(':w'), key('enter')) ),
    void_rule(start='quit', action=lambda: (key('escape'), letters(':qa'), key('enter')) ),
    void_rule(start='buffer next', action=lambda: (key('escape'), letters(':bn'), key('enter')) ),
    void_rule(start='buffer previous', action=lambda: (key('escape'), letters(':bp'), key('enter')) ),
    void_rule(start='buffers', action=lambda: (key('escape'), letters(':b '), key('tab')) ),
    void_rule(start='reload', action=lambda: (key('escape'), letters(':e'), key('enter')) ),
    void_rule(start='status', action=lambda: (key('escape'), key('control'), letters('g')) ),
    void_rule(start='undo', action=lambda: (key('escape'), letters('u')) ),
    void_rule(start='redo', action=lambda: (key('escape'), hold('ctrl'), letters('R')) ),
    void_rule(start='block select', action=lambda: (key('escape'), letters('V')) ),
    void_rule(start='select', action=lambda: (key('escape'), letters('v')) ),
]

python = [
    ('assign', ' = '),
    ('add', ' + '),
    ('subtract', ' - '),
    ('times', ' * '),
    ('divide', ' / '),
    ('element', ', '),
    ('class', 'class '),
    ('function', 'def '),
    ('pass', 'pass'),
    ('call', '('),
    ('end call', ')'),
    ('dictionary', '{'),
    ('end dictionary', '}'),
    ('list', '['),
    ('end list', ']'),
    ('index', '['),
    ('end index', ']'),
]
python_rules = []
def python_inner(name, text):
    quoted = (
        re.sub('\\[', '\\\\[', 
        re.sub('\\]', '\\\\]', 
        text)))
    python_rules.append(void_rule(start=name, action=lambda: (letters(text)) ))
    python_rules.append(void_rule(start='next ' + name, action=lambda: (key('slash'), letters(quoted), key('enter')) ))
    python_rules.append(void_rule(start='back ' + name, action=lambda: (key('question'), letters(quoted), key('enter')) ))
for name, text in python:
    python_inner(name, text)
grammars.append({
    'condition': re.compile('\\.py .*- VIM$'),
    'rules': base + terminal_rules + vim_rules + python_rules
})
grammars.append({
    'condition': re.compile('- VIM$'),
    'rules': base + terminal_rules + vim_rules,
})
grammars.append({
    'condition': re.compile('Vimperator'),
    'rules': base + [
        integer_rule(start='go', action=lambda value: letters(str(value)) ),
        void_rule(start='find', action=lambda: (key('escape'), letters('f')) ),
        void_rule(start='tab new', action=lambda: (key('escape'), letters('tabopen about:blank'), key('enter')) ),
        void_rule(start='tab find', action=lambda: (key('escape'), letters('F')) ),
        void_rule(start='tab next', action=lambda: (key('escape'), hold('control'), key('pgdown')) ),
        void_rule(start='tab back', action=lambda: (key('escape'), hold('control'), key('pgup')) ),
        void_rule(start='tab close', action=lambda: (key('escape'), hold('control'), letters('w')) ),
        void_rule(start='refresh', action=lambda: (hold('control'), key('r')) ),
        void_rule(start='window new', action=lambda: (key('escape'), letters(':winopen about:blank'), key('enter')) ),
        void_rule(start='window find', action=lambda: (key('escape'), letters(';w')) ),
        void_rule(start='address', action=lambda: (key('escape'), hold('control'), letters('l')) ),
        void_rule(start='copy', action=lambda: (hold('control'), letters('c')) ),
        void_rule(start='paste', action=lambda: (hold('control'), letters('v')) ),
        void_rule(start='bookmark show', action=lambda: (key('escape'), hold('control'), letters('b')) ),
        void_rule(start='bookmark new', action=lambda: (key('escape'), letters(':dialog addbookmark'), key('enter')) ),
    ]
})
grammars.append({
    'condition': re.compile('^Terminal -'),
    'rules': base + terminal_rules + [
        void_rule(start='external paste', action=lambda: (hold('control'), letters('V')) ),
        void_rule(start='cancel', action=lambda: (hold('control'), letters('c')) ),
        void_rule(start='end of file', action=lambda: (hold('control'), letters('d')) ),
        void_rule(start='vim', action=lambda: (letters('vim ')) ),
        void_rule(start='source status', action=lambda: (letters('git status'), key('enter')) ),
        void_rule(start='source clone', action=lambda: (letters('git clone ')) ),
        void_rule(start='source branch', action=lambda: (letters('git checkout -b ')) ),
        void_rule(start='source commit', action=lambda: (letters('git commit -a'), key('enter')) ),
        void_rule(start='source show branches', action=lambda: (letters('git branch -a'), key('enter')) ),
        void_rule(start='source difference', action=lambda: (letters('git diff'), key('enter')) ),
        void_rule(start='source pull', action=lambda: (letters('git pull'), key('enter')) ),
        void_rule(start='source push', action=lambda: (letters('git push'), key('enter')) ),
        void_rule(start='less', action=lambda: (letters('less ')) ),
        void_rule(start='follow', action=lambda: (letters('tail -F ')) ),
        void_rule(start='path home', action=lambda: (letters('cd'), key('enter')) ),
        void_rule(start='path up', action=lambda: (letters('cd ..'), key('enter')) ),
        void_rule(start='path down', action=lambda: (letters('cd ..')) ),
        void_rule(start='path go', action=lambda: (letters('cd ')) ),
        void_rule(start='list files', action=lambda: (letters('ls -1 | cat -n | less'), key('enter')) ),
        void_rule(start='stash line', action=lambda: (hold('control'), letters('u')) ),
        void_rule(start='restore line', action=lambda: (hold('control'), letters('y')) ),
        integer_rule(start='file', action=lambda value: (letters('$(ls -1 | tail -n+{} | head -n 1)'.format(value))) ),
    ],
})
grammars.append({
    'condition': re.compile(''),
    'rules': base,
})

