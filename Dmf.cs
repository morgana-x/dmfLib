using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaibanDataLib
{
    // ALl this code genuinely sucks I'm sorry :(
    public class Dmf
    {
        public static string MagicByte = "DMF "; // Identifier
        public FileStream dataStream;

        public List<string> filePaths = new List<string>(); // Should Probably have this as a class but....
        public List<int> fileSizes = new List<int>();
        public List<int> filePositions = new List<int>();

        public int NumberOfFiles;
        public static bool IsDMF(FileStream datStream) // Checks if this is a legitimate DMF file
        {
            datStream.Position = 0;

            byte[] bufferName = new byte[MagicByte.Length]; // Attempt to get magic bytes

            datStream.Read(bufferName);

            string ApparantName = System.Text.Encoding.GetEncoding("Shift-JIS").GetString(bufferName);

            if (MagicByte != ApparantName) // Check if the string of the identifier bytes is equivalent to the identifier of the data file
            {
                return false;
            }
            return true;
        }
        public bool IsDMF()
        {
            return IsDMF(this.dataStream);
        }
        public byte[] GetFileData(int i) // Get the actual file Data
        {
            int fileOffset = filePositions[i];
            string filePath = filePaths[i];
            //int fileSize = fileSizes[i]; this is unreliable!

            // Since I can't find the filesize properlly within the file's entries, I'm just going "screw it" and using the differences of fileOffsets
            // I'm sorry for the ineligant solution D:

            int fileSize = ((i < fileSizes.Count - 1) ? filePositions[i + 1] : (int)dataStream.Length) - fileOffset;

            /*if (fileSize - 1 < 0)
            {
                return null;
            }*/

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
            byte[] fileData = GetFileData(i);
            string filePath = filePaths[i];
            filePath = filePath.Replace("/", "\\");
            string[] filePathParts = filePath.Split('\\');
            string thing = "";
            int e = 0;
            //Console.WriteLine(filePath);
            foreach (string part in filePathParts)
            {
                e++;
                if (e == filePathParts.Length)
                {
                    break;
                }
                thing += "\\" + part;
                string newDir = outDirectory + "\\dataExtract\\" + thing;
                //Console.WriteLine(newDir);
                if (!Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                }
            }

            string newPath = outDirectory + "\\dataExtract\\" + filePath;
            if (fileData.Length == 0)
            {
                //Console.WriteLine("Error filedata length 0: " + filePath);
                File.Create(newPath);
                return;
            }
            if (!newPath.EndsWith(".txt") && !newPath.EndsWith(".scn"))
            {
                File.WriteAllBytes(newPath, fileData);
            }
            else
            {
                string japaneseText = System.Text.Encoding.GetEncoding("Shift-JIS").GetString(fileData);
                File.WriteAllText(newPath, japaneseText);
            }
        }
        public void ExtractFiles(string outDirectory) // Todo: Clean this stuff up!
        {
            if (filePaths.Count < 1)
            {
                ReadFileMetaData();
            }
            if (Directory.Exists(outDirectory))
            {
                Directory.Delete(outDirectory, true);
            }
            if (!Directory.Exists(outDirectory))
            {
                Directory.CreateDirectory(outDirectory);
            }
            for (int i = 0; i < filePaths.Count; i++)
            {
                string filePath = filePaths[i];
                byte[] fileData = GetFileData(i);
                if (fileData == null) 
                {
                    continue;
                }
            
                filePath = filePath.Replace("/", "\\");
                string[] filePathParts = filePath.Split('\\');
                string thing = "";
                int e = 0;
                //Console.WriteLine(filePath);
                foreach (string part in filePathParts)
                {
                    e++;
                    if (e == filePathParts.Length)
                    {
                        break;
                    }
                    thing += "\\" + part;
                    string newDir = outDirectory + thing;
                    //Console.WriteLine(newDir);
                    if (!Directory.Exists(newDir))
                    {
                        Directory.CreateDirectory(newDir);
                    }
                }

                string newPath = outDirectory + filePath;

                if (fileData.Length == 0)
                {
                    //Console.WriteLine("Error filedata length 0: " + filePath);
                    File.Create(newPath);
                    continue;
                }
                File.WriteAllBytes(newPath, fileData);
                /*
                if (!newPath.EndsWith(".txt") && !newPath.EndsWith(".scn"))
                {
                    File.WriteAllBytes(newPath, fileData);
                }
                else
                {
                    string japaneseText = System.Text.Encoding.GetEncoding("Shift-JIS").GetString(fileData);
                    File.WriteAllText(newPath, japaneseText);
                }*/



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
                byte[] fileNameLengthBuffer = new byte[1]; // Read the length of the file path string
                dataStream.Read(fileNameLengthBuffer);
                int fileNameLength = fileNameLengthBuffer[0];


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
        public static void Pack(string folder) // temp
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
            Console.WriteLine("Number of files: " + tempfilePaths.Count);
            for (int i =0; i < tempfileData.Count; i++)
            {
                long fileLoc = tempDMFFile.Position;
                tempFileLocations.Add(tempDMFFile.Position);
                int fileSize = tempfileData[i].Length;
                /*if (tempfilePaths[i].EndsWith(".txt") || tempfilePaths[i].EndsWith(".spn"))
                {
                    fileSize = Encoding.GetEncoding("Shift-JIS").GetString(tempfileData[i]).Length;
                }*/
                tempDMFFile.Write(tempfileData[i]);
               
                long currentLoc = tempDMFFile.Position;

                tempDMFFile.Position = tempFileMetaLocations[i];
                //tempDMFFile.Position += (tempfilePaths[i].Length) + 4 + 5;
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
        public Dmf(FileStream stream)
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
