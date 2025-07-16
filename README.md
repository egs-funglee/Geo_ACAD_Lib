# Geo_ACAD_Lib
AutoCAD .NET Assembly - ‘Geo_AC2016.dll’ for EGS Geo  
This DLL which comes with the CRS Processing Helper contains several high-performance AutoCAD functions.  
It DLL can be loaded via NETLOAD command in AutoCAD 2015 - 2024. It has only been tested with AutoCAD 2016 and 2022.

#### Profile Commands
|||
|---|---|
|Plot_RF_Profile|Plot RF with specified RPL and CXYZ (Multi-threaded)|

#### North-Up Commands
|||
|---|---|
|Plot_PUFI_NU|Plot PC/CNV/FENWDKPRO files|
|Plot_RF_NU|Plot RF files in NU view for cutting/trimming|
|Fetch_RF_NU_to_Files|Convert trimmed Reflectors from NU back to RF files and save to Desktop|
|Plot_TG|Plot TG files|
|Insert_Geotiff|Insert Geotiff images|
| Fix_Polylines | Erase Zero-Length Polylines. Fix Closed Polylines (by appending 1st Vertex and the end) for CAD. Erase invalid Vertex.|


#### Compile using VS2022
Target .NET Framework v4.8 (and it will work with AutoCAD 2015-2024; 2025-2026 needs to rebuild with .NET 8.0)  
[Ref: Autodesk AutoCAD 2026 - Managed .NET Compatibility](https://help.autodesk.com/view/OARX/2026/ENU/?guid=GUID-A6C680F2-DE2E-418A-A182-E4884073338A)


##### The Assembly was built with reference : AutoCAD 2016 ObjectARX SDK - AcCoreMgd, AcDbMgd and AcMgd
```path
C:\ObjectARX 2016\inc\
or
.\ACAD_ObjectARX2016_inc\
```
The Changelog is updated in the CRS Processing Helper.  
The DLL is being built by the GitHub Action with the tag: workflow - [Release Page](https://github.com/egs-funglee/Geo_ACAD_Lib/releases)
