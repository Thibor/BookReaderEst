namespace NSProgram
{
    internal class CHeader
    {
        public int oblivion = 0;
        public const string name = "BookReaderEst";
        public const string version = "2024-12-11";
        public const string extension = "est";

        public string Title()
        {
            return $"{name} {version}";
        }

        public string ToStr()
        {
            return $"{name} {version} {oblivion}";
        }

        public bool FromStr(string s)
        {
            string[] a = s.Split();
            if (a.Length > 2)
                int.TryParse(a[2], out oblivion);
            return (a[0] == name) && (a[1] == version);
        }

    }
}
