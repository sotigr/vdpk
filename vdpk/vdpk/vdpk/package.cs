using System;
using System.Collections.Generic;
using System.IO;
namespace vdpk
{
    public class package : Dictionary<string, file>, IDisposable
    {

        //Menber vars
        private int f_count = 0;
        private string _file_path;
        private bool _v_s;
        public Dictionary<string, List<file>> OldFileVersions;

        //Properties
        public string PackagePath { get { return _file_path; } }

        //Events
        public event EventHandler ProgressChanged;

        protected virtual void OnProgressChanged(PackageEventArgs e)
        {
            EventHandler handler = ProgressChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        //Constructor
        public package(string path, bool use_versioning)
        {
            _file_path = path;
            _v_s = use_versioning ;
            if (use_versioning) {
                OldFileVersions = new Dictionary<string, List<file>>();
            }
        }
        public package(string path )
        {
            _file_path = path;
            _v_s = false;
        }
        //It appends files to a package.
        //If the package did not previusly exist on the disk
        //it is nessesary to call the MakePackage method to
        //create a template of the package in the disk, before appending any files.
        public void AppendFile(file f)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(_file_path, FileMode.Append)))
            {
                if (_v_s)
                {
                    if (ContainsKey(f.RealPath + f.RealName))
                    { 
                        if (f.Version <= this[f.RealPath + f.RealName].Version)
                        { 
                            f.Version += this[f.RealPath + f.RealName].Version - f.Version + 1;
                        } 
                    }
                } 
                writer.Write((f.IsPathEncrypted) ? (byte)49 : (byte)48);
                writer.Write((f.IsEncrypted) ? (byte)49 : (byte)48);
                writer.Write((f.IsCompressed) ? (byte)49 : (byte)48); 
                writer.Write(f.Version); 
                writer.Write(f.Path);
                writer.Write(f.Name);
                writer.Write(f.Data.Length);
                writer.Write(f.Data);
                writer.Close();
            }
            f = null;
        }

        //It saves the package to the disk.
        //If an instance of this class does not contain any files,
        //this fuction can be used to create a template
        //for the package and then manualy use the Append function
        //to add files to the package.
        public void MakePackage()
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(_file_path, FileMode.Create)))
            {
                writer.Write("SFA");
                foreach (KeyValuePair<string, file> entry in this)
                {
                    //Writes the file header
                    if (_v_s)
                    {
                        if (ContainsKey(entry.Value.RealPath + entry.Value.RealName))
                        {
                            if (entry.Value.Version <= this[entry.Value.RealPath + entry.Value.RealName].Version)
                            {
                                entry.Value.Version += this[entry.Value.RealPath + entry.Value.RealName].Version - entry.Value.Version + 1;
                            }
                        }
                    }
                    writer.Write((entry.Value.IsPathEncrypted) ? (byte)49 : (byte)48);  //
                    writer.Write((entry.Value.IsEncrypted) ? (byte)49 : (byte)48);      // Here we write the encryption and compression flags
                    writer.Write((entry.Value.IsCompressed) ? (byte)49 : (byte)48);     // 
                    writer.Write(entry.Value.Version);
                    writer.Write(entry.Value.Path);
                    writer.Write(entry.Value.Name);
                    writer.Write(entry.Value.Data.Length);
                    writer.Write(entry.Value.Data); //Writes the file's data
                }
                writer.Close();
            }
        }
        //This is the fast way to request files
        //it seeks the data of the file based on its data index,
        //LoadFileNames needs to be called before this function can be used.
        public file GetFile(string name)
        {
            try
            {
                file myfile = this[name];//Get the file from the dictionary to obtain the header parameters
                using (BinaryReader reader = new BinaryReader(File.Open(this._file_path, FileMode.Open)))// open the package in the disk
                {
                    reader.BaseStream.Seek(myfile.DataIndex, SeekOrigin.Begin);//Seek to the data index
                    myfile.SetEncryptedData(reader.ReadBytes(myfile.Size));    //Fetch and set the data
                }
                return myfile;
            }
            catch { return null; }
        }

        public file GetFile(file inputfile)
        {
            try
            { 
                using (BinaryReader reader = new BinaryReader(File.Open(this._file_path, FileMode.Open)))// open the package in the disk
                {
                    reader.BaseStream.Seek(inputfile.DataIndex, SeekOrigin.Begin);//Seek to the data index
                    inputfile.SetEncryptedData(reader.ReadBytes(inputfile.Size));    //Fetch and set the data
                }
                return inputfile;
            }
            catch { return null; }
        }
        //Will load all files in the package to the memory
        //except from their data, to reduce memory consumption.
        //It must be called before calling the GetFile method.
        public void LoadFileNames(bool version_sensitive)
        {
            _v_s = version_sensitive;
            using (BinaryReader reader = new BinaryReader(File.Open(this._file_path, FileMode.Open)))
            {
                f_count = 0;//This is the file couter.
                if (reader.ReadString() != "SFA")
                    throw new System.InvalidOperationException("Invalid package file");//The file starts with a string SFA, otherwise the file is invalid.

                while (reader.BaseStream.Length != reader.BaseStream.Position)//If the current stream position is not the end of the file.
                {
                    long f_inx_pos = reader.BaseStream.Position;// Read the file index.

                    //Reading the header
                    byte _enc_f_p = reader.ReadByte(); //
                    byte _enc = reader.ReadByte();     //Read encryption and compression flags.
                    byte _comp = reader.ReadByte();    //
                    int _f_version = reader.ReadInt32();

                    string path = reader.ReadString(), // Read the file's path
                           fname = reader.ReadString();// and the file's name
                    int buffer = reader.ReadInt32();   // read file's size.
                    file f = new file((_enc == (byte)49) ? true : false,     //
                                      (_comp == (byte)49) ? true : false,    //Create new file class and pass the encryption flags
                                      (_enc_f_p == (byte)49) ? true : false);//
                    f.Version = _f_version;
                    f.Package = _file_path;     //Set the package path
                    f.SetEncryptedName(fname);  //Set the raw name to be handled
                    f.SetEncryptedPath(path);   //Set the raw path to be handled

                    //Bellow we resolve the format of the path
                    string name_res;
                    if (f.RealPath.EndsWith("/"))
                    {
                        name_res = f.RealPath + f.RealName;
                    }
                    else
                    {
                        name_res = f.RealPath + "/" + f.RealName;
                    }
                    f.Size = buffer; //Set the size
                    f.DataIndex = reader.BaseStream.Position; //Set the data index
                    f.FileIndex = f_inx_pos;                  //Set the file index 
                    if (ContainsKey(name_res))//Check if the file is already loaded.
                    {  
                        if (version_sensitive)
                        {
                            if (!OldFileVersions.ContainsKey(name_res))
                                OldFileVersions[name_res] = new List<file>(); 
                            if (this[name_res].Version < _f_version)
                            {
                                OldFileVersions[name_res].Add(this[name_res]); 
                            } 
                        } 
                    }
                    this[name_res] = f;
                    reader.BaseStream.Seek(buffer, SeekOrigin.Current);//Skip the rest of the file to reduce memory consumption.
                    f_count += 1;
                }
            }
             GC.Collect();
        }

        public void LoadFileNames()
        {
            LoadFileNames(false);
        }

        //Will go through the entire package looking for the file.
        //Use this function only if nessesary and only when you are
        //not sure if the file index or data index of your file is valid.
        public file GetFileSlow(string name)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(_file_path, FileMode.Open)))
                {
                    if (reader.ReadString() != "SFA")
                        throw new System.InvalidOperationException("Invalid package file");//The file starts with a string SFA, otherwise the file is invalid.

                    while (reader.BaseStream.Length != reader.BaseStream.Position)
                    {
                        long f_del_pos = reader.BaseStream.Position;
                        byte _enc_f_p = reader.ReadByte();
                        byte _enc = reader.ReadByte();
                        byte _comp = reader.ReadByte();
                        int _f_version = reader.ReadInt32();

                        string path = reader.ReadString(),
                               fname = reader.ReadString();
                        int buffer = reader.ReadInt32();
                        file f = new file((_enc == (byte)49) ? true : false,
                                          (_comp == (byte)49) ? true : false,
                                          (_enc_f_p == (byte)49) ? true : false);
                        f.Version = _f_version;
                        f.Package = this._file_path;

                        f.SetEncryptedName(fname);
                        f.SetEncryptedPath(path);
                        string name_res;
                        if (f.RealPath.EndsWith("/"))
                        {
                            name_res = f.RealPath + f.RealName;
                        }
                        else
                        {
                            name_res = f.RealPath + "/" + f.RealName;
                        }
                        if (name == name_res)
                        {
                            f.DataIndex = reader.BaseStream.Position;
                            f.SetEncryptedData(reader.ReadBytes(buffer));
                            f.FileIndex = f_del_pos;
                            return f;
                        }
                        else
                        {
                            reader.BaseStream.Seek(buffer, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch { return null; }
            return null;
        }


        //Very slow on large packages. It will copy the whole package except from the
        //requested file. Its the only way to completely remove a portion of the package.
        public void DeleteFile(string filename)
        {

            LoadFileNames();
            //Creates a new package with the same name as the
            //original except that it is followed by the ~ character.
            using (BinaryWriter writer = new BinaryWriter(File.Open(_file_path + "~", FileMode.Create)))
            {
                writer.Write("SFA");
                writer.Close();
            }
            //Writes all the files of the current package to the new one.
            package p = new package(_file_path + "~");
            int filecount = Count;
            int currentfile = 0;
            foreach (KeyValuePair<string, file> pair in this)
            {
                currentfile += 1;
                if (filename != pair.Key)
                {
                    p.AppendFile(GetFile(pair.Key));
                }
                OnProgressChanged(new PackageEventArgs() { Percentage = (int)(100 * currentfile / filecount) });
            }
            p.Dispose();
            p = null;
            //   GC.Collect();

            //Deletes the current package
            //and removes the ~ infront of the new package
            //to match the name of the deleted package
            try
            {
                File.Delete(_file_path);
                File.Move(_file_path + "~", _file_path);
            }
            catch
            {
                System.Threading.Thread.Sleep(1000);
                File.Delete(_file_path);
                File.Move(_file_path + "~", _file_path);
            }

        }

        //Searches for the file int the dictionary.
        public bool FileExists(string file_path)
        {
            if (this.ContainsKey(file_path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Returns the number of files loaded from the stored package.
        //this property is updated only when loading from the disk and
        //not when adding a new file. To get the current file count access
        //the inherited dictionary.
        public int FileCount
        {
            get
            {
                return f_count;
            }
        }

        //Clears all files from memmory.
        public void Dispose()
        {
            foreach (KeyValuePair<string, file> entry in this)
            {
                entry.Value.Dispose();
            }
            this.Clear();
            //GC.Collect();
        }

    }
    //This class is used to pass custom arguments through the event calls.
    public class PackageEventArgs : EventArgs
    {
        public int Percentage;
    }
}