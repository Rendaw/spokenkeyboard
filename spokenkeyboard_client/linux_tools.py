from subprocess import Popen, PIPE, run
import re

def do_linux(*args):
    run(['xdotool'] + list(args))

linux_map = dict([
    ('enter', 'Return'),
    ('pgup', 'Page_Up'),
    ('pgdown', 'Page_Down'),
    ('up', 'Up'),
    ('down', 'Down'),
    ('left', 'Left'),
    ('right', 'Right'),
    ('home', 'Home'),
    ('end', 'End'),
    ('escape', 'Escape'),
    ('tab', 'Tab'),
    ('backspace', 'BackSpace'),
    ('delete', 'Delete'),
    ('|', 'bar'),
    ('-', 'minus'),
    ('.', 'period'),
    (',', 'comma'),
    ('\\', 'backslash'),
    ('_', 'underscore'),
    ('*', 'asterisk'),
    (':', 'colon'),
    (';', 'semicolon'),
    ('@', 'at'),
    ('"', 'quotedbl'),
    ('\'', 'apostrophe'),
    ('#', 'numbersign'),
    ('$', 'dollar'),
    ('%', 'percent'),
    ('&', 'ampersand'),
    ('/', 'slash'),
    ('=', 'equal'),
    ('+', 'plus'),
    (' ', 'space'),
    ('(', 'parenleft'),
    (')', 'parenright'),
] + [
    ('f' + str(x), 'F' + str(x)) for x in range(1, 12)
])

holds = set()
def clear_holds():
    for key in list(holds):
        letgo(key)

def key(name):
    print('key ' + name)
    do_linux('key', linux_map.get(name, name))
    clear_holds()

def toggle(name):
    if holds:
        clear_holds()
    else:
        hold(name)

def letgo(name):
    print('lift ' + name)
    do_linux('keyup', linux_map.get(name, name))
    if name in holds:
        holds.remove(name)

def hold(name):
    print('hold ' + name)
    do_linux('keydown', linux_map.get(name, name))
    holds.add(name)

def letters(string):
    do_linux('type', string)
    clear_holds()

def switch_desktop(num):
    do_linux('set_desktop', str(num))

def get_desktop():
    return run(['xdotool', 'get_desktop'], stdout=PIPE).stdout.decode('utf-8')

def raise_window(name):
    do_linux('search', '--all', '--limit', '1', '--desktop', get_desktop(), '--name', name, 'windowactivate')

def top_title():
    for line in Popen(['xprop', '-root', '_NET_ACTIVE_WINDOW'], stdout=PIPE).stdout:
        m = re.search('^_NET_ACTIVE_WINDOW.* ([\w]+)$', line.decode('utf-8'))
        if m != None:
            id_ = m.group(1)
            id_w = Popen(['xprop', '-id', id_, 'WM_NAME'], stdout=PIPE)
            if id_w != None:
                for line in id_w.stdout:
                    match = re.match("WM_NAME\(\w+\) = (?P<name>.+)$", line.decode('utf-8'))
                    if match != None:
                        return match.group("name")[1:-1]
            break
    return 'unknown'
