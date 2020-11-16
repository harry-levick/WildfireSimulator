import os.path
import pathlib
import pandas as pd
import rasterio as rio
from common.constants import *
from flask import Flask, Response
from flask import jsonify, request

app = Flask(__name__)

geodata = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
geodata_classes = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSES)

dataset = rio.open(geodata)
classes = pd.read_csv(geodata_classes)




@app.route('/model-code', methods=['GET'])
def get_model_code() -> Response:
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json array containing the model code for
            each coordinate given
    """
    if not request.json:
        return Response(status=400)

    output_codes = []
    try:
        for feature in request.json:
            lat, long = feature['geometry']['coordinates']

            try:
                [value] = dataset.sample([(long, lat)])
                code = value.item()
            except ValueError:
                return Response(status=500)

            # coordinate given out of bounds
            if int(code) not in classes['number'].values:
                raise ValueError

            output_codes.append(value.item())

    except (KeyError, ValueError): # body not correctly formatted
        return Response(status=400)

    return jsonify(output_codes), 200

