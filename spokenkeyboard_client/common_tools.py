current_grammar = None
push_lookup = None
class Action:
    last = None
action = Action()

stack = []
def push_grammar(name):
    stack.append(current_grammar)
    switch_grammar(push_lookup[name])

def pop_grammar():
    switch_grammar(stack.pop())

def void_rule(start, action, norepeat=False):
    return { 'type': 'VoidRule', 'start': start, 'action': action, 'norepeat': norepeat  }

def integer_rule(start, action, norepeat=False):
    return { 'type': 'SingleIntegerRule', 'start': start, 'action': action, 'norepeat': norepeat  }

def string_rule(start, action, norepeat=False):
    return { 'type': 'SingleStringRule', 'start': start, 'action': action, 'norepeat': norepeat }

switch_grammar = None
