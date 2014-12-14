namespace ODB
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(/*string[] args*/)
        {
            using (ODBGame game = new ODBGame())
            {
                game.Run();
            }
        }
    }
#endif
}

