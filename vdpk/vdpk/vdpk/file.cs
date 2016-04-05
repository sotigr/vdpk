using System;
namespace vdpk
{
    public class file : IDisposable
    {
        //Compression and Encryption Algorithms

        public IEncryption Cryptography = new EncryptionAES();
        public ICompression Compression = new CompressionGZIP();
     
        //Member Variables   
         
        private byte[] _bytes;
        private int _size;
        private long _pos_in_pack;
        private long _pos_del;
        private string _dname;
        private string _dpath;
        private string _name;
        private bool _compressed_data;
        private bool _encrypted_data;
        private bool _encrypted_path_name;
        private string _package;
        private string _path;
        private int _version;
          
        //Properties

        public int Size { set { _size = value; } get { return _size; } } 
        public string Package { set { _package = value; } get { return _package; } }
        public long DataIndex { set { _pos_in_pack = value; } get { return _pos_in_pack; } }
        public long FileIndex { set { _pos_del = value; } get { return _pos_del; } }
        public string RealName { set { _dname = value; } get { return _dname; } }
        public string RealPath { set { _dpath = value; } get { return _dpath; } }
        public bool IsCompressed { set { _compressed_data = value; } get { return _compressed_data; } }      
        public bool IsEncrypted { set { _encrypted_data = value; } get { return _encrypted_data; } }
        public bool IsPathEncrypted { set { _encrypted_path_name = true; } get { return _encrypted_path_name; } }
        public int Version { set { _version = value; } get { return _version; } }

        public string Name
        {   set
            { 
                if (_encrypted_path_name)
                {
                    _name = Cryptography.Encrypt(value, Settings.DefaultPassword);
                }
                else
                {
                    _name = value;
                }
                _dname = value;
            }
            get
            {
                return _name;
            }
        }

        public string Path
        {
            set
            {
                if (_encrypted_path_name)
                {
                    _path = Cryptography.Encrypt(value, Settings.DefaultPassword);
                }
                else
                {
                    _path = value;
                }
                _dpath = value;
            }
            get
            {
                return _path;
            }
        }

        public byte[] Data
        {
            set
            {

                if (_encrypted_data && _compressed_data)
                {
                    _bytes = Compression.Compress(Cryptography.Encrypt(value, Settings.DefaultPassword));
                }
                else if (_encrypted_data)
                {
                    _bytes = Cryptography.Encrypt(value, Settings.DefaultPassword);
                }
                else if (_compressed_data)
                {
                    _bytes = Compression.Compress(value);
                }
                else
                {
                    _bytes = value;
                }

                value = null;
               // GC.Collect();
            }
            get
            {
                return _bytes;
            }
        }

        //Setters Getters
        public void SetEncryptedData(byte[] data)
        {
            _bytes = data;
            data = null;
        }
        public void SetEncryptedName(string nm)
        {
            if (_encrypted_path_name)
            {
                _dname = Cryptography.Decrypt(nm, Settings.DefaultPassword);
            }
            else
            {
                _dname = nm; 
            }
            _name = nm;
        }
        public void SetEncryptedPath(string pth)
        {
            if (_encrypted_path_name)
            {
                _dpath = Cryptography.Decrypt(pth, Settings.DefaultPassword);
            }
            else
            {
                _dpath = pth;
            }
            _name = pth;
        }
        public byte[] GetDecryptedData()
        {
            byte[] bt;
            if (_encrypted_data && _compressed_data)
            {
                bt = Cryptography.Decrypt(Compression.Decompress(_bytes), Settings.DefaultPassword);
            }
            else if (_encrypted_data)
            {
                bt = Cryptography.Decrypt(_bytes, Settings.DefaultPassword);
            }
            else if (_compressed_data)
            {
                bt = Compression.Decompress(_bytes);
            }
            else
            {
                bt = _bytes;
            }

           // GC.Collect();
            return bt;
        }
      

        //Constructors
        public file(bool encrypt_data, bool compress_data, bool encrypt_paths_names)
        {
            _encrypted_data = encrypt_data;
            _compressed_data = compress_data;
            _encrypted_path_name = encrypt_paths_names;
            _version = 0;
        }
        public file(byte[] inbytes, string name, string path, string package, bool encrypt_data, bool compress_data, bool encrypt_paths_names)
        {
            _path = path;
            _name = name;
            Data = inbytes;
            _size = _bytes.Length;
            inbytes = null;
            _encrypted_data = encrypt_data;
            _compressed_data = compress_data;
            _encrypted_path_name = encrypt_paths_names;
            _package = package; 
            _version = 0;
        }
        public file(byte[] inbytes, string name, string path, string package, IEncryption Crypto, bool encrypt_data, bool compress_data, bool encrypt_paths_names)
        {
            _path = path;
            _name = name;
            Data = inbytes;
            _size = _bytes.Length;
            inbytes = null;
            Cryptography = Crypto;
            _encrypted_data = encrypt_data;
            _compressed_data = compress_data;
            _encrypted_path_name = encrypt_paths_names;
            _package = package;
            _version = 0;
        }
        public file(string name, string path, string package, bool encrypt_data, bool compress_data, bool encrypt_paths_names)
        {
            _path = path;
            _name = name;
            _encrypted_data = encrypt_data;
            _compressed_data = compress_data;
            _encrypted_path_name = encrypt_paths_names;
            _package = package;
            _version = 0;
        }
        public file(string name, string path, string package, IEncryption Crypto, bool encrypt_data, bool compress_data, bool encrypt_paths_names)
        {
            _path = path;
            _name = name;
            Cryptography = Crypto;
            _encrypted_data = encrypt_data;
            _compressed_data = compress_data;
            _encrypted_path_name = encrypt_paths_names;
            _package = package;
            _version = 0;
        }

        //Dispose
        public void Dispose()
        {
            _bytes = null;
            _name = null;
            _path = null;
         //   GC.Collect();
        }

    }
}