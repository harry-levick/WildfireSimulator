import os.path
import pathlib
import pandas as pd
import rasterio as rio
from flask import Flask, Response
from flask import jsonify, request
from src.common.constants import *

app = Flask(__name__)

scott_burgan_classified = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
scott_burgan_classes = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSES)
scott_burgan_class_fuel_load = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_FUEL_LOAD)
scott_burgan_class_sav_ratio = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_SAV_RATIO)

raster_dataset = rio.open(scott_burgan_classified)
model_classes = pd.read_csv(scott_burgan_classes)
model_classes_fuel_load = pd.read_csv(scott_burgan_class_fuel_load)
model_classes_sav_ratio = pd.read_csv(scott_burgan_class_sav_ratio)


@app.route('/model-number', methods=['GET'])
def get_model_code() -> Response:
    """
    :body Takes a Json list body that contains
            a single GeoJson object for each coordinate
    :return: a Json array containing the model number for
            each coordinate given
    """
    if not request.json:
        return Response(status=400)

    output_numbers = []
    try:
        for feature in request.json:
            lat, long = feature['geometry']['coordinates']

            try:
                [value] = raster_dataset.sample([(long, lat)])
                number = value.item()
            except ValueError:
                return Response(status=500)

            # coordinate given out of bounds
            if int(number) not in model_classes['number'].values:
                raise ValueError

            output_numbers.append(value.item())

    except (KeyError, ValueError): # body not correctly formatted
        return Response(status=400)

    return jsonify(output_numbers), 200


@app.route('/model-parameters', methods=['GET'])
def get_model_parameters() -> Response:
    """
    :query_parameter Takes a model code
    :return: a Json array containing all of the fuel model
            parameters associated with the given model code
    """
    parameter_name = "number"
    parameters = request.args

    if parameter_name not in parameters:
        return Response(status=400)

    model_number = int(parameters[parameter_name])

    if model_number not in model_classes[parameter_name].values:
        return Response(status=400)

    [parameters] = model_classes.loc[model_classes[parameter_name] == model_number].to_dict('r')

    fuel_load = model_classes_fuel_load.loc[model_classes_fuel_load[parameter_name] == model_number]\
                    .drop(labels=['code', 'number'], axis=1)\
                    .values\
                    .tolist()[0]
    fuel_load = list(map(float, fuel_load))
    parameters["fuel load"] = fuel_load

    sav_ratio = model_classes_sav_ratio.loc[model_classes_sav_ratio[parameter_name] == model_number]\
                    .drop(labels=['code', 'number'], axis=1)\
                    .values\
                    .tolist()[0]
    sav_ratio = list(map(float, sav_ratio))
    parameters["sav ratio"] = sav_ratio

    return jsonify(parameters), 200
