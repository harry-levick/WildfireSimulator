import geopandas as gpd
import os.path
import pathlib
import rasterio as rio
from common.constants import *
from flask import Flask, Response
from flask import jsonify, request
from shapely.geometry import Point

app = Flask(__name__)

geotiff = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
dataset = rio.open(geotiff)


@app.route('/model-code', methods=['GET'])
def get_model_code() -> Response:
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json array containing the model code for
            each coordinate given
    """
    if not request.json:
        return Response(status=200)

    input_coordinates = []
    try:
        for feature in request.json:
            lat, long = feature['geometry']['coordinates']
            input_coordinates.append(Point(long, lat))
    except:
        return Response(status=400)

    if not input_coordinates:
        return Response(status=400)

    # convert the (long, lat) points in degrees to the CRS being used by the dataset
    points_df = gpd.GeoDataFrame(input_coordinates, columns=['geometry'], crs=WGS84).to_crs(ALBERS_EQUAL_AREA_CONIC)

    codes = []
    for index, row in points_df.iterrows():
        for [val] in dataset.sample([(row.geometry.centroid.x, row.geometry.centroid.y)]):
            codes.append(str(val))

    return jsonify(codes), 200

