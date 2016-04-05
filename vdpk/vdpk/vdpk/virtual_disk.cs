using System;
using System.Collections.Generic;
using System.IO;
namespace vdpk
{
    public class virtual_disk : Dictionary<string, byte[]>, IDisposable
    {
        //Member variables
        private string _path = null;// The location of the packages in the disk
        private bool _loaded = false;
        private List<package> p_list = new List<package>();//The loaded packages
        private int f_count = 0;// Total file count

        //Properties
        public List<package> Packages { get { return p_list; } }
        public int FileCount { get { return f_count; } }

        //Constructors
        public virtual_disk(string path_to_pack)
        {
            _path = path_to_pack;
        }
        public virtual_disk(string path_to_pack, bool load)
        {
            _path = path_to_pack;
            if (load)
            {
                Load();
            }
        }

        //Methods

        //Loads all packages in the requested directory,
        //indicated by the extension .sfa
        public bool Load(bool version_sensitive)
        {
            try
            {
                f_count = 0;
                p_list.Clear();
                foreach (string filepath in Directory.GetFiles(_path, "*.sfa"))
                {
                    package p = new package(filepath, version_sensitive);
                    p.LoadFileNames(version_sensitive);
                    p_list.Add(p);
                    f_count += p.FileCount;
                }
                _loaded = true;
                GC.Collect();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Load(bool version_sensitive, List<string> load_order)
        {
            try
            {
                f_count = 0;
                p_list.Clear();
                foreach (string filepath in load_order)
                {
                    package p = new package(filepath, version_sensitive);
                    p.LoadFileNames(version_sensitive);
                    p_list.Add(p);
                    f_count += p.FileCount;
                }
                _loaded = true;
                GC.Collect();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Load()
        {
            return Load(false);
        }
        //This comes from an older version of the system.
        //The package name is stored in the file now and it is
        //more optimal to get it from there.
        public string GetFilePackageName(string fileName)
        {
            if (_loaded)
            {
                foreach (package p in p_list)
                {
                    if (p.FileExists(fileName))
                    {
                        return Path.GetFileName(p.PackagePath);
                    }
                }
                return "";
            }
            return "";
        }

        //Appends a file to a package.
        //This method will save the file to the disk.
        public void AddFile(file mfile)
        {
            foreach (package p in p_list)
            {
                if (p.PackagePath == mfile.Package)
                {
                    p.AppendFile(mfile);
                }
            }
        }

        //It is suggested to uses the overloaded version of this method, bellow...
        public file ReadFile(string filename)
        {
            if (_loaded)
            {
                foreach (package p in p_list)
                {
                    if (p.FileExists(filename))
                    {
                        file f = p.GetFile(filename);
                        return f;
                    }
                }
                return null;
            }
            return null;
        }

        // If the LoadinMemory flag is raised then the file will be
        // be read from the disk only once. The next time it is going to
        // be accessed, its data will be retrieved from the inherited dictionary.
        // It is possible to directly access the dictionary to get a file to avoid
        // all the processing of this method does.
        public file ReadFile(string filename, bool LoadInMemory)
        {
            if (_loaded)
            {
                if (LoadInMemory)
                {
                    if (!this.ContainsKey(filename))
                    {
                        foreach (package p in p_list)
                        {
                            if (p.FileExists(filename))
                            {
                                this[filename] = p.GetFile(filename).GetDecryptedData();
                                return null;
                            }
                        }
                        return null;
                    }
                    return null;
                }

                foreach (package p in p_list)
                {
                    if (p.FileExists(filename))
                    {

                        file f = p.GetFile(filename);

                        return f;
                    }
                }
                return null;
            }
            return null;
        }
        public file ReadFile(file inputheader, bool LoadInMemory)
        {
            if (_loaded)
            {
                if (LoadInMemory)
                { 
                    foreach (package p in p_list)
                    {
                        if (inputheader.Package == p.PackagePath)
                        {
                            this[inputheader.RealPath + inputheader.RealName] = p.GetFile(inputheader).GetDecryptedData();

                        } 
                    }
                    return null;
                }

                foreach (package p in p_list)
                {
                    if (inputheader.Package == p.PackagePath)
                    {
                        file f = p.GetFile(inputheader);

                        return f;
                    }
                }
                return null;
            }
            return null;
        }

        //Get only the file header information
        public file ReadFileNoData(string filename)
        {
            if (_loaded)
            {
                foreach (package p in p_list)
                {
                    if (p.FileExists(filename))
                    {
                        file f = p[filename];
                        return f;
                    }
                }
                return null;
            }
            return null;
        }
        public List<file> GetOldFileVersions(string filename, string packagepath)
        {
            try
            {
                foreach (package p in p_list)
                {
                    if (p.PackagePath == packagepath)
                    {
                        return p.OldFileVersions[filename];
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        public bool FileExists(string filename)
        {
            if (_loaded)
            {
                foreach (package p in p_list)
                {
                    if (p.FileExists(filename))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        public List<string> GetAllNames()
        {
            List<string> l = new List<string>();
            foreach (package p in p_list)
            {
                foreach (KeyValuePair<string, file> kp in p)
                {
                    l.Add(kp.Key);
                }
            }
            return l;
        }

        public List<string> GetFolderContent(string path)
        {
            List<string> l = new List<string>();
            foreach (package p in p_list)
            {
                foreach (KeyValuePair<string, file> kp in p)
                {
                    if (kp.Value.RealPath == path)
                    {
                        l.Add(kp.Key);
                    }
                }
            }
            return l;
        }

        public List<string> GetFolderFolders(string path)
        {
            List<string> l = new List<string>();
            if (!path.EndsWith("/"))
            { path += "/"; }
            foreach (package p in p_list)
            {
                foreach (KeyValuePair<string, file> kp in p)
                {
                    if (kp.Value.RealPath.StartsWith(path) && kp.Value.RealPath != path)
                    {
                        string folder;

                        folder = getstringUntil(kp.Key, path.Length, "/");
                        if (!l.Contains(folder))
                        {
                            l.Add(folder);
                        }
                    }
                }
            }
            return l;
        }

        //Very slow on big archives avoid if possible.
        public void DeleteFiles(string filename)
        {
            foreach (package p in p_list)
            {
                if (Path.GetFileName(p.PackagePath) == GetFilePackageName(filename))
                {
                    p.DeleteFile(filename);
                }
            }
        }
        public void Dispose()
        {
            foreach (package p in p_list)
            {
                p.Dispose();
            }
        }

        // Utility 
        private string getstringUntil(string input, int start, string character)
        {
            try
            {
                string curchar = "";
                string final = "";
                for (int i = start; i < input.Length; i++)
                {
                    curchar = input.Substring(i, 1);
                    if (curchar != character)
                    {
                        final += curchar;
                    }
                    else
                    {
                        return final;
                    }
                }
                return final;
            }
            catch
            {
                return "";
            }

        }
    }
}
