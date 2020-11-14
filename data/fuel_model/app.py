import geopandas as gpd
import os.path
import pathlib
import rasterio as rio
from common.constants import *
from flask import Flask, Response
from flask import jsonify, request
from shapely.geometry import Point
import traceback

app = Flask(__name__)

geotiff = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
dataset = rio.open(geotiff)


@app.route('/model-code', methods=['GET'])
def get_model_code() -> Response:
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json list containing the model code for
            each coordinate given
    """
    if not request.json:
        return Response(status=200)

    coordinates = []
    for feature in request.json:
        lat, long = feature['geometry']['coordinates']
        coordinates.append(Point(long, lat))

    if not coordinates:
        Response(status=400)

    coordinates_df = gpd.GeoDataFrame(coordinates, columns=['geometry'], crs=WGS84)\
                        .to_crs(ALBERS_EQUAL_AREA_CONIC)

    model_codes = []
    for index, row in coordinates_df.iterrows():
        sample = dataset.sample([(row.geometry.centroid.x, row.geometry.centroid.y)])
        model_codes.append(str(list(sample)[0][0]))

    return jsonify(model_codes), 200

