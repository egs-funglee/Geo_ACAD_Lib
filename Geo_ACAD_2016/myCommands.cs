using Autodesk.AutoCAD.Runtime;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Geo_AC2016.MyCommands))]

namespace Geo_AC2016
{
    public class MyCommands
    {
        [CommandMethod("Extract_Line_and_Fix_for_Strip_Chart")]
        public void Extract_Line_and_Fix_for_Strip_Chart()
        {
            NU_Intersect_to_LN_Fix.Extract_Line_and_Fix_for_Strip_Chart();
        }

        [CommandMethod("Fetch_Fix_to_CSV_Profile")]
        public void Fetch_Fix_to_CSV_Profile()
        {
            SBP_Fetch_Fix_to_CSV.Fetch_Fix_to_CSV_Profile();
        }

        [CommandMethod("Fetch_RF_NU_to_Files")]
        public void Fetch_RF_NU_to_Files()
        {
            NU_Fetch_RF_to_Files.Fetch_RF_NU_to_Files();
        }

        [CommandMethod("Fix_Polylines")]
        public void Fix_Polylines()
        {
            Polyline1.Fix_Polylines();
        }

        [CommandMethod("Insert_Geotiff")]
        public void Insert_Geotiff()
        {
            NU_Insert_Geotiff.Insert_Geotiff();
        }

        [CommandMethod("Plot_Fix_from_CSV_NU")]
        public void Plot_Fix_from_CSV_NU()
        {
            NU_Plot_Fix_from_CSV.Plot_Fix_from_CSV_NU();
        }

        [CommandMethod("Plot_Fix_from_CSV_Profile")]
        public void Plot_Fix_from_CSV_Profile()
        {
            SBP_Plot_Fix_from_CSV.Plot_Fix_from_CSV_Profile();
        }

        //NorthUp Commands
        [CommandMethod("Plot_PUFI_NU")]
        public void Plot_PUFI_NU()
        {
            NU_Plot_PUFI.Plot_PUFI_NU();
        }

        [CommandMethod("Plot_RF_NU")]
        public void Plot_RF_NU()
        {
            NU_Plot_RF_NU.Plot_RF_NU();
        }

        //Profile Commands
        [CommandMethod("Plot_RF_Profile")]
        public void Plot_RF_Profile()
        {
            SBP_Plot_RF.Plot_RF_Profile();
        }

        [CommandMethod("Plot_TG")]
        public void Plot_TG()
        {
            NU_Plot_TG.Plot_TG();
        }
    }
}