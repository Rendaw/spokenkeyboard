from setuptools import setup

setup(
    name = 'spokenkeyboard-client',
    version = '0.0.1',
    author = 'Rendaw',
    author_email = 'spoo@zarbosoft.com',
    url = 'https://github.com/Rendaw/spokenkeyboard',
    download_url = 'https://github.com/Rendaw/spokenkeyboard/tarball/v0.0.1',
    license = 'BSD',
    description = 'Register and execute macros for voice commands',
    long_description = 'Register and execute macros for voice commands',
    classifiers = [
        'Development Status :: 3 - Alpha',
        'License :: OSI Approved :: BSD License',
    ],
    install_requires = [
        'appdirs',
        'twisted',
    ],
    packages = [
        'spokenkeyboard_client', 
    ],
    entry_points = {
        'console_scripts': [
            'spokenkeyboard_client = spokenkeyboard_client.main:main',
        ],
    },
)
