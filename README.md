# FoxliBot
New Discord bot made to replace DLBot

# Installation : 
  - Install discord.py 1.0 : `pip install -U git+https://github.com/Rapptz/discord.py@rewrite#egg=discord.py[voice]`
  - You need to update pip with the required modules 
  - Just get the token from Discord
  - Put the token in a file called 'keyfile.key' in JSON ('key':'aRandomToken')
  - Launch the BOT
  - TADAAAAA !

But honestly, why would you run this bot if mine is already running ?

# Use : 
Click on the link below to make the bot join a server :
 https://discordapp.com/oauth2/authorize?&client_id=324471059816382464&scope=bot


# Commands
- connect       connects you to your voice channel
- play          plays a youtube video (might work with some other audio sources too)
- pause/resume  Actions on the audio player
- skip/volume   Actions on the audio player
- playing       Displays the current song
- version       Displays the version of the bot
- choose        Choose between a list of words
- roll          Classical dice rolling
- status        Changes the status of the bot

THE END


# Changelog
I somehow skipped v3 that had a new framework based on the one did by Rapptz but it was very unstable

## [ Version 2.0 ]
###   [ 2.0a ]
- Main version
###   [ 2.0b ]
- Sound corrections 
- StackOverflow correction

## [ Version 4.1]
###   [ 4.1a ]
- New Framework. Rewrote it completely to work with discord.py v1.0
- Reconnect when network errors
- New functions
- Removed the addaudio and playaudio functions for now
- New Roll display to replace old DLBot
- Removing messages to avoid spamming
- Status system that will change uning the current song's title
- New audio system independent from the server/guild
- Performance improvements


## TO DO
Install script ! 