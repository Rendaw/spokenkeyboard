from spokenkeyboard_client.common_tools import *

# Forward to platform tools
import platform
if platform.system() == 'Linux':
    from spokenkeyboard_client.linux_tools import *
else:
    raise AssertionError('Unsupported platform: ' + platform.system())

