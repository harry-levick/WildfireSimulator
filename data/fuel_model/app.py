import geopandas as gpd
import rasterio as rio
from data.common.constants import *
from flask import Flask
from flask import request
from shapely.geometry import Point
import json
import os.path
import pathlib

app = Flask(__name__)

geotiff = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
dataset = rio.open(geotiff).read()


@app.route('/get-model-code', methods=['POST'])
def get_model_code():
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json list containing the model code for
            each coordinate given
    """
    coordinates = request.json
    for feature in coordinates:
        coord = feature['geometry']['coordinates']

    return 'OK'

