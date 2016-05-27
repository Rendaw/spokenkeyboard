This is a voice macro system that uses a Windows computer's voice recognition API.

There is a client and server.  The server runs on Windows and connects to the API.  The client sends grammars to the server and interprets recognized speech to execute macros.

##Server installation

Binary available here: https://github.com/Rendaw/spokenkeyboard/releases

Either build the solution file with Visual Studio or just run spokenkeyboard.exe.  Unless specified as the first command line parameter, a default interface and port is used.

##Client installation
Note: currently only Linux is supported

###Linux

Run `pip3 install -e https://Rendaw/spokenkeyboard`.

`xdotool` must also be installed.

Run `spokenkeyboard_client`.

###Configuration

To specify the server host and port and customize your macros, either edit the included `definition.py` or copy it to `~/.config/spokenkeyboard/definition.py` and edit it there.  The configuration should be self explanatory.

