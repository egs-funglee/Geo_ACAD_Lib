# Geo_ACAD_Lib
AutoCAD .NET Assembly for Geo
The complied ‘Geo_AC2016.dll’ which comes with the CRS Processing Helper contains several high-performance AutoCAD functions.
The DLL can be loaded via NETLOAD command in AutoCAD 2016/2022.

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
Target .NET Framework v4.8
##### AcCoreMgd, AcDbMgd and AcMgd using AutoCAD 2016 ObjectARX SDK
```path
C:\ObjectARX 2016\inc\
```
It has been tested with AutoCAD 2016 and 2022.
