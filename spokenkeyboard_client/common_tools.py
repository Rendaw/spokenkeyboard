current_grammar = None
push_lookup = None

repeat = 1
def set_repeat(value):
    global repeat
    repeat = min(100, max(1, value))
    print('repeat next ' + str(repeat))

stack = []
def push_grammar(name):
    stack.append(current_grammar)
    switch_grammar(push_lookup[name])

def pop_grammar():
    switch_grammar(stack.pop())

def void_rule(start, action):
    return { 'type': 'VoidRule', 'start': start, 'action': action }

def integer_rule(start, action):
    return { 'type': 'SingleIntegerRule', 'start': start, 'action': action }

def string_rule(start, action):
    return { 'type': 'SingleStringRule', 'start': start, 'action': action }

switch_grammar = None
