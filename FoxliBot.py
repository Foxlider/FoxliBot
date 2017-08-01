
## Notes


# Url to connect tht bot to a server: https://discordapp.com/oauth2/authorize?&client_id=324471059816382464&scope=bot
# id 324471059816382464


# Basic informations. To change if you want to setup your own Bot.

__program__ = "FoxliBot"
__version__ = "2.0b"

from pprint import pprint
from inspect import getmembers
import discord
import asyncio
import aiohttp
from discord import opus
from discord.ext import commands
from bs4 import BeautifulSoup as BS
import random
import os
from json import load as jload

OPUS_LIBS = ['libopus-0.x86.dll', 'libopus-0.x64.dll', 'libopus-0.dll', 'libopus.so.0', 'libopus.0.dylib']

description = '''An example bot to showcase the discord.ext.commands extension
module.
There are a number of utility commands being showcased here.'''

name='foxlibot'

prefix = '**'

def read_key():
    """
    Read a bot's key JSON to get it's token
    Keys must be stored in the key folder and have a basic 'key':'<keytext>' object
    """
    with open('keys/foxlibot.key', 'r') as f:
        datum = jload(f)
        key = datum.get("key", "")
        if not key:
            raise IOError("Key not found in JSON keyfile")
        return key
    return None

global datadir
datadir = os.path.dirname("./data/")
if not os.path.exists(datadir):
    os.makedirs(datadir)
audio = os.path.dirname("./data/audio")
if not os.path.exists(audio):
    os.makedirs(audio)
    
def logMsg():
    dir = os.path.dirname("./logs/")
    if not os.path.exists(dir):
            os.makedirs(dir)
    today = datetime.datetime.now()
    logsFile=open(dir+"/"+str(today)+".txt","a")
    log = "oui"
    logsFile.write(log)
    print(log,end='')
        
def get_cmd_message(uid=None):
    """
    Search the given channel for the last message
    aka: the command that was given to the bot
    """
    if len(bot.messages) == 0:
        raise Exception("Wat")
    c_uid = lambda u, v: True
    if uid is not None:
        c_uid = lambda u, v: u == v
    res = [msg for msg in bot.messages
            if msg.channel == chan
            and msg.author.id != bot.user.id
            and c_uid(uid, msg.author.id)]
    return res[-1]    
    
def get_last_message(uid=None):
    """
    Search the given channel for the second-to-last message
    aka: the message before the command was given to the bot
    """
    if len(bot.messages) == 0:
        raise Exception("Wat")
    if len(bot.messages) == 1:
        return None
    c_uid = lambda u, v: True
    if uid is not None:
        c_uid = lambda u, v: u == v
    res = [msg for msg in bot.messages
            if  msg.author.id != bot.user.id
            and c_uid(uid, msg.author.id)]
    if len(res) <= 1:
        return None
    return res[-2]
    
def load_opus_lib(opus_libs=OPUS_LIBS):
    """
    Load opus libs for voice handling
    """
    if opus.is_loaded():
        return True

    for opus_lib in opus_libs:
        try:
            opus.load_opus(opus_lib)
            return
        except OSError:
            pass

##Many Functions : 


URLMAP = {"+": "%2B",
            " ": "+", 
            "%": "%25", 
            "&": "%26", 
            "@": "%40", 
            "#": "%23", 
            "$": "%24", 
            "=": "%3D"}

def replace(string="", char_map=URLMAP):
    """
    Used to convert special chars in links for the ytsearch
    """
    "Replace a string with URL safe characters (' ' => '%20')"
    s = string
    for k, v in char_map.items():
        s = s.replace(k, v)
    return s


    return voice
#https://youtu.be/I67v_OaB3k8

def closePlayer(player, voice):
    """
    Close player
        player : player to close
        voice = deprecated
    """
    print('Closing player...')
    action = player.stop()
    print('Player closed.')
    
##ASYNC joinChannel
async def joinChannel(channel):
    """
    Automaticly join a voice channel
        channel = the voice channel to join
    """
    print('Joining '+str(channel))
    try:
        voice = await bot.join_voice_channel(channel)
        print('CONNECTED')
    except Exception as error:
        print('TimeoutError : '+str(error))
    return voice
        
##ASYNC playYtVid
async def playYtVid(target, link):
    """
    Play a youtube video's audio
    Will play a sound if the link is incorrect
        target = channel to join
        link = video link
    """
    voice = await joinChannel(target)
    print('Launching audio stream...')
    try : 
        player = await voice.create_ytdl_player(link, use_avconv=False)
    except Exception as error:
        try:
            print(error)
            print('==PLAYING FIRST ERROR FILE==')
            file = 'Spy_no0'+str(random.randint(1,3))+'.wav'
            print(file)
            player = voice.create_ffmpeg_player(file, use_avconv=False)
        except Exception as error:
            print(error)
            print('==PRINT DEFAULT NOPE==')
            player = await voice.create_ytdl_player('https://youtu.be/fxYOC3gDe7k', use_avconv=False)
    player.volume= 0.5
    #pprint(getmembers(player))
    print('Starting player')
    player.start()
    try:
        waittime = player.duration
    except:
        waittime = 1
    print(waittime)
    while not player.is_done():
        await asyncio.sleep(waittime)
    player.stop()
    print ('Disconnecting...')
    await voice.disconnect()
    print('DISCONNECTED')
    
##ASYNC playAudioFile
async def playAudioFile(target, file='yee.wav'):
    """
    Plays and audio file located in data/audio
        target = channel to join
        file = played file
    """
    voice = await joinChannel(target)
    dict = 'data/audio/'
    print('Launching audio stream...')
    try:
        player = voice.create_ffmpeg_player(dict+file, use_avconv=False)
    except:
        player = voice.create_ffmpeg_player(dict+'yee.wav', use_avconv=False)
    print('Starting player')
    player.volume=0.5
    player.start()
    while not player.is_done():
        await asyncio.sleep(1)
    action = player.stop()
    print ('Disconnecting...')
    await voice.disconnect()
    print('DISCONNECTED')
    
##ASYNC searchvid
async def searchvid(ctx, src):
    if not src:
        return bot.say("I can't search an empty text")
    print(src)
    tube = "https://www.youtube.com"
    query = tube + "/results?search_query=" + replace(src)
    print(query)
    async with aiohttp.get(query) as resp:
        if resp.status != 200:
            return await bot.say("Failed to retrieve search. STATUS:"+str(resp.status))

    # Build a BS parser and find all Youtube links on the page
        txt = await resp.text()
        bs = BS(txt, "html.parser")
        main_d = bs.find('div', id='results')
        if not main_d:
            return bot.say('Failed to find results')
        items = main_d.find_all("div", class_="yt-lockup-content")
        if not items:
            return await bot.say("No videos found")
        # Loop until we find a valid non-advertisement link
        for container in items:
            href = container.find('a', class_='yt-uix-sessionlink')['href']
            if href.startswith('/watch'):
                return await bot.say(tube+href)        
        return await bot.say("No YouTube video found")
        
##BOT and async functions

bot = commands.Bot(command_prefix='!', description=description)

@bot.event
async def on_ready():
    print('Logged in as')
    print(bot.user.name)
    print(bot.user.id)
    print('------')
    

##CMD roll
@bot.command()
async def roll(dice : str):
    """Rolls a dice in NdN format."""
    try:
        rolls, limit = map(int, dice.split('d'))
    except Exception:
        await bot.say('Format has to be in NdN!')
        return

    result = ', '.join(str(random.randint(1, limit)) for r in range(rolls))
    await bot.say(result)
    
    
##CMD choose
@bot.command(description='For when you wanna settle the score some other way')
async def choose(*choices : str):
    """Chooses between multiple choices."""
    await bot.say(random.choice(choices))


##CMD joined
@bot.command()  
async def joined(member : discord.Member):
    """Says when a member joined."""
    await bot.say('{0.name} joined in {0.joined_at}'.format(member))
##CMD status
@bot.command()
async def status(status='I am DLBot but better'):
    """Set a new status"""
    await bot.change_presence(game=discord.Game(name=status))

##CMD cool
@bot.group(pass_context=True)
async def cool(ctx):
    """Says if a user is cool.
    In reality this just checks if a subcommand is being invoked.
    """
    if ctx.invoked_subcommand is None:
        await bot.say('No, {0.subcommand_passed} is not cool'.format(ctx))
@cool.command(name='bot')
async def _bot():
    """Is the bot cool?"""
    await bot.say('Yes, the bot is cool.')
    
##CMD shutdown
@bot.command()
async def shutdown():
    """
    Kill the process
    """
    bot.logout()
    bot.close()
    exit()

##CMD ytplay
@bot.command(pass_context=True)
async def ytplay(ctx, src=""):
    """
    Will play a youtube video's audio
    """
    if not src or src=="":
        return bot.say("I can't search an empty text")
    if "youtu" not in src:
        return bot.say('Use a valid link')
    print(src)
    target = ctx.message.author.voice_channel
    load_opus_lib()
    await playYtVid(target, src)
    
##CMD audioplay
@bot.command(pass_context=True)
async def audioplay(ctx, src=""):
    """
    Will play a file
    """
    if not src or src=="":
        return bot.say("I can't play an empty text")
    target = ctx.message.author.voice_channel
    load_opus_lib()
    await playAudioFile(target, src)

##CMD addaudio
@bot.command(pass_context=True)
async def addaudio(ctx, src=""):
    """
    Will add an audio file
    """
    #pprint(getmembers(ctx.message.attachments))
    #print(ctx.message.attachments)
    for att in ctx.message.attachments:
        print(att['url'])
        link = att['url']
        async with aiohttp.get(link) as response:
            filename = att['filename']
            with open('data/audio/'+filename, 'wb') as f_handle:
                print('Loading file :\n[')
                while True:
                    print('#',end='')
                    chunk = await response.content.read(1024)
                    if not chunk:
                        break
                    f_handle.write(chunk)
                print(']\nFile downloaded')  
            await bot.say(filename + ' created ! Call it using `!audioplay '+filename+'`')
            return await response.release()
                
    
#FIXME broken
##CMD ytsearch
@bot.command(pass_context=True)
async def ytsearch(ctx, src=""):
    """
    Get the first Youtube search result video
    Example: !yt how do I take a screenshot
    """
    origChan = ctx.message.channel
    await bot.send_typing(origChan)
    src = ctx.message.content.replace('!ytsearch ','')
    await searchvid(ctx, src)

##CMD ytsearchplay
@bot.command(pass_context=True)
async def ytsearchplay(ctx, src=""):
    """
    Get the first Youtube search result video and play it
    Example: !yt how do I take a screenshot
    """
    bot.delete_message(ctx.message)
    target = ctx.message.author.voice_channel
    origChan = ctx.message.channel
    await bot.send_typing(origChan)
    src = ctx.message.content.replace('!ytsearchplay ','')
    mgs = await searchvid(ctx, src)
    load_opus_lib()
    src = mgs.content
    bot.delete_message(mgs)
    await playYtVid(target, src)
    
    
bot.run(read_key())
