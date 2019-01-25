using Sharpy.Properties;

namespace Sharpy
{
    internal class SharpyConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public SharpyConfiguration()
        {
            this.Prefix = "..";
            if (Settings.Default.DEV_MODE)
                this.Tokens.Discord = "NTM4MzA2ODIxMzMzNzEyOTE2.DyyA9A.NR6IO59ORsQIcqVtY7jWxrH-IAo";
            else
                this.Tokens.Discord = "";
        }
    }
    internal class Tokens
    {
        public string Discord { get; set; }
    }
}