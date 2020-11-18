import os.path
import pathlib
import rasterio as rio
from flask import Flask, Response
from flask import jsonify, request
from src.common.constants import *

app = Flask(__name__)

lfmc_classified = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MOISTURE_MAP)
raster_dataset = rio.open(lfmc_classified)


@app.route('/live-fuel-moisture-content', methods=['GET'])
def get_live_fuel_moisture() -> Response:
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json array containing the fuel moisture
            value for each coordinate given
    """
    if not request.json:
        return Response(status=400, response="GeoJson incorrectly formatted.")

    output_numbers = []
    try:
        for feature in request.json:
            lat, long = feature['geometry']['coordinates']
            # latitude outside of the bounds -90 <= x <= +90
            # longitude outside of the bounds -180 <= x <= +180
            if (lat < -90) or (lat > 90) or (long < -180) or (long > 180):
                return Response(status=400, response=f"Invalid coordinate lat:{lat},long:{long}")
            try:
                [value] = raster_dataset.sample([(long, lat)])
            except ValueError:
                return Response(status=500, response=f"Exception in fetching raster value for lat:{lat}, long:{long}")

            output_numbers.append(value.item())

    except (KeyError, ValueError): # body not correctly formatted
        return Response(status=400, response="GeoJson incorrectly formatted.")

    return jsonify(output_numbers), 200