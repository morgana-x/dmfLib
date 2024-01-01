using SaibanDataLib;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Encodings.Web;
public partial class Program
{
    /*
     First 3 bytes: DMF (Identifier of filetype)
     At position 4, 4 bytes: Number of files (Int 16)
     At position 8, file entries begin, see below
     */


    /* File Entry
     * 4 Bytes : (Length of FilePath) (Integer 16)
     * (Length Of FilePath) Bytes: (File Path) (String)
     * 4 Bytes : File Offset Within archive (Integer 32)
     * 4 Bytes : File Size (Int 16 / ushort ) // STILL WORKING ON THIS PART!
     * */

    /*
     * After file Entries raw uncompressed filedata
     */
    public static bool MainLoop(string[] args)
    {
        Console.Clear();
        Console.Title = "DMF Extract Repack";
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.OutputEncoding = Encoding.Unicode; //Encoding.GetEncoding("Shift-JIS");
        Console.InputEncoding = Encoding.Unicode;


        Console.ForegroundColor = ConsoleColor.Gray;
        string selectedFile = "";

        if (args.Length < 1)
        {
            Console.WriteLine("Enter or drag and drop the file you want to extract\nOR\nEnter or drag and drop the folder you want to repack");
            selectedFile = Console.ReadLine().Replace("\"", string.Empty);
        }
        else
        {
            selectedFile = args[0];
        }

        string outDirectory = Directory.GetParent(selectedFile).FullName;

        if (Directory.Exists(selectedFile)) // If its a folder, assume they want to repack it into a .dat / DMF file
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Packing...");

            Dmf.Pack(selectedFile);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Packed!");


            return false;
        }

        // Assume it is a DMF file they want to extract contents from 

        Dmf DmfFileInstance = new Dmf(selectedFile);

        if (!DmfFileInstance.IsDMF()) // Check if it's an actual DMF file
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Provided File is not a DMF File!");


            return false;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Extracting Files...");

        DmfFileInstance.ExtractAllFiles(outDirectory + selectedFile.Substring(selectedFile.LastIndexOf("\\")).Replace(".dat", "") + "_extracted\\");
        DmfFileInstance.Dispose(); // Clear all data and close any streams
        DmfFileInstance = null; // A bit overkill

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Extracted Files!");

        return false;
    }
    public static void Main(string[] args)
    {
       while (true)
       {
            if (MainLoop(args))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press any button to quit...");
                Console.ReadKey();
                break;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press any button to continue...");
                Console.ReadKey();
            }
            
       }

    }
}
