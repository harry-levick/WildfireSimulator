# NASA Earthdata
To model the terrain in this project, I have used elevation data collected from NASA Earthdata. The script `download.sh` can be used to download all 68 'tiles' that all make up the height map data for California. To run this script, use the username `H_lev1` with password `!XBQ+**sH4bz9@_`. 

Then, with the tiles downloaded, run the following `gdal` command:

```
gdal_merge.py -ot Float32 -of GTiff -o /Users/harrylevick/Documents/GitHub/WildfireSimulator/data/src/topology/ca_heightmap.tif --optfile /private/var/folders/cc/dq3kfgd956n0lb4vzggjzk_80000gn/T/processing_vgxnMw/80aee8de67754ec2a89e1195bc769fe8/mergeInputFiles.txt
```

With this merged geotiff, convert to a .raw format that is required by unity, using the following `gdal` command:

```
gdal_translate -ot "UInt16" -of "ENVI" -scale -outsize 1025 1025 ca_heightmap.tif ca_heightmap.raw
```