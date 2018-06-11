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
import sys
from json import load as jload

OPUS_LIBS = ['libopus-0.x86.dll', 'libopus-0.x64.dll', 'libopus-0.dll', 'libopus.so.0', 'libopus.0.dylib']

description = '''FoxliBot being FoxliBot, expect me to crash.'''

name='foxlibot'

__program__ = "FoxliBot"
__version__ = "3.1b"

sampledir = 'data/audio/'

prefix = '!'


### ______________
### Main Functions

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


### ____________
### Data Classes

class VoiceEntry:
    def __init__(self, message, player):
        self.requester = message.author
        self.channel = message.channel
        self.player = player

    def __str__(self):
        try: 
            fmt = '*{0.title}*'
        except:
            fmt = '*audio sample*'
        try : 
            duration = self.player.duration
        except:
            duration = False
        if duration:
            fmt = fmt + ' [{0[0]}m {0[1]}s]'.format(divmod(duration, 60))
        return fmt.format(self.player, self.requester)

class VoiceState:
    def __init__(self, bot):
        self.current = None
        self.voice = None
        self.bot = bot
        self.play_next_song = asyncio.Event()
        self.songs = asyncio.Queue()
        self.skip_votes = set() # a set of user_ids that voted
        self.audio_player = self.bot.loop.create_task(self.audio_player_task())

    def is_playing(self):
        if self.voice is None or self.current is None:
            return False

        player = self.current.player
        return not player.is_done()

    @property
    def player(self):
        return self.current.player

    def skip(self):
        self.skip_votes.clear()
        if self.is_playing():
            self.player.stop()

    def toggle_next(self):
        self.bot.loop.call_soon_threadsafe(self.play_next_song.set)

    async def audio_player_task(self):
        while True:
            self.play_next_song.clear()
            self.current = await self.songs.get()
            await self.bot.send_message(self.current.channel, 'Now playing ' + str(self.current))
            self.current.player.start()
            await self.play_next_song.wait()


### _________
### Bot class

class Music:
    """Voice related commands.

    Works in multiple servers at once.
    """
    def __init__(self, bot):
        self.bot = bot
        self.voice_states = {}
        load_opus_lib()

    def get_voice_state(self, server):
        state = self.voice_states.get(server.id)
        if state is None:
            state = VoiceState(self.bot)
            self.voice_states[server.id] = state
        return state


    ##CMD shutdown
    @commands.command(passgett=True)
    async def shutdown(self, ctx):
        """
        Kill the process
        """
        await ctx.invoke(self.stop)
        print("Shutting down...")
        await bot.say("Shutting down...")
        bot.logout()
        print("Logged out...")
        bot.close()
        print("Closed")
        quit()

    ##CMD cool
    @commands.group(pass_context=True)
    async def cool(self, ctx):
        """Says if a user is cool.
        In reality this just checks if a subcommand is being invoked.
        """
        print("Command 'cool' called")
        bot.delete_message(ctx.message)
        if ctx.invoked_subcommand is None:
            await bot.say('No, {0.subcommand_passed} is not cool'.format(ctx))
    @cool.command(name='bot')
    async def _bot(self):
        """Is the bot cool?"""
        await bot.say('Yes, the bot is cool.')

    ##CMD status
    @commands.command(pass_context=True, no_pm=True)
    async def status(self, ctx, *,status='I am DLBot but better'):
        """Set a new status"""
        print('!status => ' + status)
        #await bot.say("Ok, changing my status to : '" + str(status) + "'")
        bot.delete_message(ctx.message)
        await bot.change_presence(game=discord.Game(name=status))

    ##CMD version
    @commands.command(pass_context=True)
    async def version(self, ctx):
        """Displays the version of the bot"""
        print("Command 'version' called")
        bot.delete_message(ctx.message)
        print("I am *{} v{}*.\nNice to meet you {}.".format(__program__, __version__, ctx.message.author.mention))
        await bot.say("I am *{} v{}*.\nNice to meet you {}.".format(__program__, __version__, ctx.message.author.mention))
    ##CMD roll
    @commands.command(pass_context=True)
    async def roll(self, ctx, dice : str):
        """Rolls a dice in NdN format."""
        print("Command 'roll' called")
        bot.delete_message(ctx.message)
        try:
            rolls, limit = map(int, dice.split('d'))
        except Exception:
            await bot.say('Format has to be in NdN!')
            return

        msg = '{} rolled {}d{}'.format(ctx.message.author.mention, rolls, limit)
        for r in range(rolls) :
            msg += "\n It's a "+str(random.randint(1, limit))+" !"
        await bot.say(msg)

        
    ##CMD choose
    @commands.command(pass_context=True, description='For when you wanna settle the score some other way')
    async def choose(self, ctx, *choices : str):
        """Chooses between multiple choices."""
        print("Command 'choose' called")
        await bot.say(random.choice(choices))


    ##CMD addaudio
    @commands.command(pass_context=True)
    async def addaudio(self, ctx, src=""):
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
                    print('Loading file ' + filename)
                    total_length = response.headers.get('content-length')
                    if total_length is None: # no content length header
                        f_handle.write(response.content)
                    else:
                        dl = 0
                        total_length = int(total_length)
                        while True:
                            chunk = await response.content.read(1024)
                            if not chunk:
                                break
                            dl += len(chunk)
                            f_handle.write(chunk)
                            done = int(50 * dl / total_length)
                            dled = round(dl/1024,2)
                            total = round(total_length/1024,2)
                            sys.stdout.write("\r[%s%s] %sKB / %sKB" % ('#' * done, ' ' * (50-done), dled, total) )    
                            sys.stdout.flush()
                    print('\nFile downloaded')  
                await bot.say(filename + ' created ! Call it using `!audioplay '+filename+'`')
                return await response.release()
    
    ##CMD volume
    @commands.command(pass_context=True, no_pm=True)
    async def volume(self, ctx, value : int):
        """Sets the volume of the currently playing song."""
        print("Command 'volume' called")
        bot.delete_message(ctx.message)
        state = self.get_voice_state(ctx.message.server)
        if state.is_playing():
            player = state.player
            player.volume = value / 100
            await self.bot.say('Volume à {:.0%}'.format(player.volume))

    ##CMD pause
    @commands.command(pass_context=True, no_pm=True)
    async def pause(self, ctx):
        """Pauses the currently played song."""
        print("Command 'pause' called")
        bot.delete_message(ctx.message)
        state = self.get_voice_state(ctx.message.server)
        if state.is_playing():
            player = state.player
            player.pause()

    ##CMD resume
    @commands.command(pass_context=True, no_pm=True)
    async def resume(self, ctx):
        """Resumes the currently played song."""
        print("Command 'resume' called")
        bot.delete_message(ctx.message)
        state = self.get_voice_state(ctx.message.server)
        if state.is_playing():
            player = state.player
            player.resume()

    ##CMD stop
    @commands.command(pass_context=True, no_pm=True)
    async def stop(self, ctx):
        """Stops playing audio and leaves the voice channel.

        This also clears the queue.
        """
        print("Command 'stop' called")
        bot.delete_message(ctx.message)
        server = ctx.message.server
        state = self.get_voice_state(server)

        if state.is_playing():
            player = state.player
            player.stop()

        try:
            state.audio_player.cancel()
            del self.voice_states[server.id]
            await state.voice.disconnect()
        except:
            pass


    ##CMD skip
    @commands.command(pass_context=True, no_pm=True)
    async def skip(self, ctx):
        """Vote to skip a song.
        """
        print("Command 'skip' called")
        bot.delete_message(ctx.message)
        state = self.get_voice_state(ctx.message.server)
        if not state.is_playing():
            await self.bot.say('Not playing...')
            return
        else :
            state.skip()
            await self.bot.say('Next song...')

    ##CMD playing
    @commands.command(pass_context=True, no_pm=True)
    async def playing(self, ctx):
        """Shows info about the currently played song."""
        print("Command 'playing' called")
        state = self.get_voice_state(ctx.message.server)
        if state.current is None:
            await self.bot.say('Not playing...')
        else:
            await self.bot.say('Playing {}'.format(state.current))

    ##CMD summon
    @commands.command(pass_context=True, no_pm=True)
    async def summon(self, ctx):
        """Summons the bot to join your voice channel."""
        print("Command 'summon' called")
        summoned_channel = ctx.message.author.voice_channel
        if summoned_channel is None:
            await self.bot.say('You are not in a vocal channel.')
            return False

        state = self.get_voice_state(ctx.message.server)
        if state.voice is None:
            state.voice = await self.bot.join_voice_channel(summoned_channel)
        else:
            await state.voice.move_to(summoned_channel)

        return True

    ##CMD audiolist
    @commands.command(pass_context=True)
    async def audiolist(self, ctx, src=""):
        """
        Display the list of audio samples
        """
        print("Command 'audiolist' called")
        try:
            txt = "Audio sample list :\n"
            for file in os.listdir("data/audio"):
                txt+="`" + str(file) + "`\n"
            print(txt)
            await bot.say(txt)
        except:
            pass

    ##CMD ytsearchplay
    @commands.command(pass_context=True)
    async def ytplay(self, ctx, *, src : str):
        """
        Get the first Youtube search result video and play it
        Example: !yt how do I take a screenshot
        """
        print("Command 'ytplay' called")
        bot.delete_message(ctx.message)

        state = self.get_voice_state(ctx.message.server)
        opts = {
            'default_search': 'auto',
            'quiet': True,
            'reconnect' : 1,
            'reconnect_streamed' : 1,
            'reconnect_delay_max' : 5,
        }

        if state.voice is None:
            success = await ctx.invoke(self.summon)
            if not success:
                return

        try:
            player = await state.voice.create_ytdl_player(src, ytdl_options=opts, after=state.toggle_next)
        except Exception as e:
            fmt = 'ALERT an error occured : ```py\n{}: {}\n```'
            await self.bot.send_message(ctx.message.channel, fmt.format(type(e).__name__, e))
            await ctx.invoke(self.stop)
        else:
            player.volume = 0.25
            entry = VoiceEntry(ctx.message, player)
            await self.bot.say(str(entry) + ' ajoutée')
            await state.songs.put(entry)

    ##CMD audioplay
    @commands.command(pass_context=True)
    async def audioplay(self, ctx, src=""):
        """
        Will play a file
        """
        print("Command 'audioplay' called")
        print(src)
        for root, dirs, files in os.walk(sampledir):  
            for filename in files:
                print(filename)
                if src in filename:
                    break
        if not src or src=="":
            return bot.say("I can't play an empty text")
        state = self.get_voice_state(ctx.message.server)

        if state.voice is None:
            success = await ctx.invoke(self.summon)
            if not success:
                return
        player = state.voice.create_ffmpeg_player(sampledir+filename, use_avconv=False)
        
        player.volume=0.5
        player.start()
        while not player.is_done():
            await asyncio.sleep(1)
        await ctx.invoke(self.stop)



### ________________
### Startup commands

bot = commands.Bot(command_prefix=commands.when_mentioned_or('!'), description=description)
bot.add_cog(Music(bot))

@bot.event
async def on_ready():
    print('Logged in as:\n{0} \n(ID: {0.id})\n------'.format(bot.user))

bot.run(read_key())
