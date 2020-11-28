import os.path
import pathlib
import rasterio as rio
from flask import Flask, Response
from flask import jsonify, request
from data.src.common.constants import *

app = Flask(__name__)

lfmc_classified = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MOISTURE_MAP)
raster_dataset = rio.open(lfmc_classified)


@app.route('/live-fuel-moisture-content', methods=['GET'])
def get_live_fuel_moisture() -> Response:
    """
    :body Takes a lat and lon
    :return: The fuel moisture value for the coordinate given
    """
    try:
        lat = float(request.args['lat'])
        lon = float(request.args['lon'])
        if (lat < -90) or (lat > 90) or (lon < -180) or (lon > 180):
            return Response(status=400, response=f"Invalid coordinate lat:{lat},long:{lon}")
        try:
            [value] = raster_dataset.sample([(lon, lat)])
        except ValueError:
            return Response(status=500, response=f"Exception in fetching raster value for lat:{lat}, long:{lon}")

        return str(value.item()), 200

    except (KeyError, ValueError): # body not correctly formatted
        return Response(status=400, response="GeoJson incorrectly formatted.")