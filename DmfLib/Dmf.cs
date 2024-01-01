using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaibanDataLib
{
    // ALl this code genuinely sucks I'm sorry :(
    // 
    public class Dmf
    {
        public static string FileSignature = "DMF "; // Identifier
        public Stream dataStream;

        public List<string> filePaths = new List<string>(); // Should Probably have this as a class but....
        private List<int> fileSizes = new List<int>();
        private List<int> filePositions = new List<int>();

        public int NumberOfFiles;
        public static bool IsDMF(Stream datStream) // Checks if this is a legitimate DMF file
        {
            datStream.Position = 0;

            byte[] bufferName = new byte[FileSignature.Length]; // Attempt to get magic bytes

            datStream.Read(bufferName);

            string ApparantName = System.Text.Encoding.GetEncoding("Shift-JIS").GetString(bufferName);

            if (FileSignature != ApparantName) // Check if the string of the identifier bytes is equivalent to the identifier of the data file
            {
                return false;
            }
            return true;
        }
        public bool IsDMF()
        {
            return IsDMF(this.dataStream);
        }
        private byte[] GetFileData(int i) // Get the actual file Data
        {
            int fileOffset = filePositions[i];
            string filePath = filePaths[i];
            //int fileSize = fileSizes[i]; this is unreliable!

            // Since I can't find the filesize properlly within the file's entries, I'm just going "screw it" and using the differences of fileOffsets
            // I'm sorry for the ineligant solution D:

            //             (If not at the end of list     Get next Item          otherwise get end of stream) Subtract with the fileoffset
            int fileSize = ((i < fileSizes.Count - 1) ? filePositions[i + 1] : (int)dataStream.Length) - fileOffset;


            byte[] fileData = new byte[fileSize];

            dataStream.Position = fileOffset; // Travel to it's offset within the file
            dataStream.Read(fileData); // Read the data into the buffer
            
            return fileData; 
        }
        public byte[] GetFileData(string path)
        {
            return GetFileData(filePaths.IndexOf(path));
        }
        public void ExtractFile(int i, string outDirectory)
        {
            string filePath = filePaths[i];
            byte[] fileData = GetFileData(i);
            filePath = filePath.Replace("/", "\\");
            string newPath = outDirectory + filePath;
            Directory.CreateDirectory(Directory.GetParent(newPath).ToString());
            if (fileData.Length == 0)
            {
                File.Create(newPath);
                return;
            }
            File.WriteAllBytes(newPath, fileData);
        }
        public void ExtractAllFiles(string outDirectory) // Todo: Clean this stuff up!
        {
            ReadFileMetaData();

            if (Directory.Exists(outDirectory))
            {
                Directory.Delete(outDirectory, true);
            }

            for (int i = 0; i < filePaths.Count; i++)
            {
                ExtractFile(i, outDirectory);
            }
        }
        public void ReadFileMetaData() // Get lists of all File Paths, Sizes and Locations
        {
            if (NumberOfFiles == null) // We rely on number of files!
            {
                ReadHeader(); 
            }


            filePaths = new List<string>();
            filePositions = new List<int>();
            fileSizes = new List<int>();


            dataStream.Position = 8; // Start at First file Entry
            // TODO: Fix file sizes

            while (filePaths.Count < NumberOfFiles)
            {
                int fileNameLength = dataStream.ReadByte();


                byte[] fileNameBuffer = new byte[fileNameLength]; // Read the actual string
                dataStream.Position += 3; // This +3 is needed to include the file extension (for some reason), if it's removed everything gets broken!
                dataStream.Read(fileNameBuffer);
                dataStream.Position -= 3;
                string filePath = System.Text.Encoding.GetEncoding("Shift-JIS").GetString(fileNameBuffer); // Convert FilePath string into Shift-JIS (Japanese character set)
                filePaths.Add(filePath);


                byte[] fileLocationBuffer = new byte[4]; // Location of the File's raw data in the Archive
                dataStream.Position += 3;
                dataStream.Read(fileLocationBuffer);
                int fileLocation = (int)BitConverter.ToInt32(fileLocationBuffer);
                filePositions.Add(fileLocation);

                byte[] tempBuffer = new byte[4]; // The file size (Completely BROKEN, I just ignore this and use the differences in file positions as a temp fix cause I dont understand this at all!)
                dataStream.Read(tempBuffer);
                int fileSize = BitConverter.ToUInt16(tempBuffer);
                fileSizes.Add(fileSize);
            }
        }
        public static void Pack(string folder) // temp, This is super screwed but I'll fix it later... Maybe (It does repack it as a 1:1 perfect replica of the original file and the games reads it perfectly so maybe not)
        {
            List<string> tempfilePaths = new List<string>();
            List<int> tempfileSizes = new List<int>();
            List<byte[]> tempfileData = new List<byte[]>();
            List<long> tempFileLocations = new List<long>();
            List<long> tempFileMetaLocations = new List<long>();
            FileStream tempDMFFile = new FileStream(folder + ".dat", FileMode.Create);
            EnumerationOptions enumuratorOptions = new EnumerationOptions();
            enumuratorOptions.RecurseSubdirectories = true;
            tempfilePaths = Directory.EnumerateFiles(folder, "*", enumuratorOptions).ToList();



            tempDMFFile.Position = 0;
            byte[] identifier = new byte[4]{ 68,77,70,32};
            tempDMFFile.Write(identifier);
            tempDMFFile.Position = 4;
            tempDMFFile.Write(BitConverter.GetBytes(tempfilePaths.Count));
            tempDMFFile.Position = 8;
            for (int i =0; i < tempfilePaths.Count; i++)
            {
          
                int fileLocation = 0;
        
              
                string newPath = tempfilePaths[i].Replace(folder + "\\", "").Replace("\\", "/");
                string oldPath = tempfilePaths[i];
                tempfilePaths[i] = newPath;
          
                tempfileData.Add(File.ReadAllBytes(oldPath));

                byte[] PathAsBytes = Encoding.GetEncoding("Shift-JIS").GetBytes(newPath.Substring(0, newPath.Length - 4));

                string extension = newPath.Substring(newPath.Length - 4);
                extension = extension.ToLowerInvariant();
                byte[] ExtensionAsBytes = Encoding.ASCII.GetBytes(extension);


                tempDMFFile.Write(BitConverter.GetBytes(PathAsBytes.Length + ExtensionAsBytes.Length));

                foreach (byte c in PathAsBytes)
                {
                    tempDMFFile.WriteByte(c);
                }

                foreach (byte c in ExtensionAsBytes)
                {
                    tempDMFFile.WriteByte(((byte)c));
                }
          
     
                int fileSize = tempfileData[i].Length;
                tempFileMetaLocations.Add(tempDMFFile.Position);
                tempDMFFile.Write(BitConverter.GetBytes(fileLocation));
                tempDMFFile.Write(BitConverter.GetBytes(fileSize));

             
            }
          
            for (int i =0; i < tempfileData.Count; i++)
            {
                long fileLoc = tempDMFFile.Position;
                tempFileLocations.Add(tempDMFFile.Position);
                int fileSize = tempfileData[i].Length;
                tempDMFFile.Write(tempfileData[i]);
               
                long currentLoc = tempDMFFile.Position;

                tempDMFFile.Position = tempFileMetaLocations[i];
                tempDMFFile.Write(BitConverter.GetBytes((int)fileLoc));
                tempDMFFile.Write(BitConverter.GetBytes(fileSize));
                tempDMFFile.Position = currentLoc;
            }

            tempDMFFile.Dispose();
            tempDMFFile.Close();


        }
        public void ReadHeader()
        {
            if (!IsDMF())
            {
                return;
            }
            dataStream.Position = 4; // Skip the DMF identifier etc
            byte[] lengthOfMetaBuffer = new byte[4];
            dataStream.Read(lengthOfMetaBuffer);
            NumberOfFiles = BitConverter.ToInt16(lengthOfMetaBuffer);
            ReadFileMetaData();
        }
        public void Dispose()
        {
            dataStream.Dispose();
            dataStream.Close();

        }
        public Dmf(Stream stream)
        {
            dataStream= stream;
            ReadHeader();
        }
        public Dmf(string path)
        {
            dataStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            ReadHeader();
        }
    }
}
