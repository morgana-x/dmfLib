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

    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.OutputEncoding = Encoding.Unicode; //Encoding.GetEncoding("Shift-JIS");
        Console.InputEncoding = Encoding.Unicode;



        if (args.Length < 1)
        {
            Console.WriteLine("Enter the location of the .dat DMF file!");
        }
        string selectedFile = (args.Length > 0) ? args[0] : Console.ReadLine().Replace("\"", string.Empty);
        string outDirectory = Directory.GetParent(selectedFile).FullName;


        Dmf DmfFileInstance = new Dmf(selectedFile);

        if (!DmfFileInstance.IsDMF()) // Check if it's an actual DMF file
        {
            Console.WriteLine("Provided File is not a DMF File!");
            Main(new string[] { }); return;
        }

        Console.WriteLine("Extracting Files to " + outDirectory + "/dataExtract/ ....");

        DmfFileInstance.ExtractFiles(outDirectory);

        Console.WriteLine("Extracted Files!");

        Main(new string[] { });

    }
}
