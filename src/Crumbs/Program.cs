class Program
{
    static void Main()
    {
        var conf = new Configuration();
        var logger = new SimpleLogger(conf.LogFile);

        var crumbs = new Crumbs(conf, logger);
        crumbs.Run("/Users/leotrim/Projects");
    }
}
