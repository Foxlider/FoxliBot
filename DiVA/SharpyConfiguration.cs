namespace DiVA
{
    internal class DiVAConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public DiVAConfiguration(string prefix = "..", Tokens token = null)
        {
            this.Prefix = prefix;
            if (DiVA.DEV_MODE)
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