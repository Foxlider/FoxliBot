namespace Sharpy
{
    internal class SharpyConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public SharpyConfiguration()
        {
            this.Prefix = "..";
            if (Sharpy.DEV_MODE)
                this.Tokens = new Tokens("NTM4MzA2ODIxMzMzNzEyOTE2.DyyA9A.NR6IO59ORsQIcqVtY7jWxrH-IAo");
            else
                this.Tokens = new Tokens();
        }
        public SharpyConfiguration(string prefix)
        {
            this.Prefix = prefix;
            if (Sharpy.DEV_MODE)
                this.Tokens = new Tokens("NTM4MzA2ODIxMzMzNzEyOTE2.DyyA9A.NR6IO59ORsQIcqVtY7jWxrH-IAo");
            else
                this.Tokens = new Tokens();
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