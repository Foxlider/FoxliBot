namespace Sharpy
{
    internal class SharpyConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public SharpyConfiguration(string prefix = "..", Tokens token = null)
        {
            this.Prefix = prefix;
            if (Sharpy.DEV_MODE)
                this.Tokens = new Tokens("NTM4MzA2ODIxMzMzNzEyOTE2.DyyA9A.NR6IO59ORsQIcqVtY7jWxrH-IAo");
            else if (token == null)
                this.Tokens = token;
            else
                this.Tokens = token;
        }
    }
    internal class Tokens
    {
        public string Discord { get; set; }
        public Tokens(string discord = "")
        {
            this.Discord = discord;
        }
    }
}