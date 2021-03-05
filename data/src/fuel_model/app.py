import data.src.common.Converters as Converters
import os.path
import pathlib
import pandas as pd
import rasterio as rio
from data.src.common.constants import *
from flask import Flask, Response
from flask import jsonify, request
from shapely.geometry import *

app = Flask(__name__)
app.url_map.converters['float'] = Converters.FloatConverter

scott_burgan_classified = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSIFIED)
scott_burgan_classes = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_CLASSES)
scott_burgan_class_fuel_load = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_FUEL_LOAD)
scott_burgan_class_sav_ratio = os.path.join(pathlib.Path(__file__).parent.absolute(), 'data', FUEL_MODEL_SAV_RATIO)

raster_dataset = rio.open(scott_burgan_classified)
model_classes = pd.read_csv(scott_burgan_classes)
model_classes_fuel_load = pd.read_csv(scott_burgan_class_fuel_load)
model_classes_sav_ratio = pd.read_csv(scott_burgan_class_sav_ratio)

control_lines = {}


@app.route('/model-number', methods=['GET'])
def get_model_code() -> Response:
    """
    :body Takes a lat and lon
    :return: the model number for the coordinate given
    """
    try:
        lat = float(request.args['lat'])
        lon = float(request.args['lon'])
        uuid = str(request.args['uuid'])

        if uuid not in control_lines:
            control_lines[uuid] = []

        point = Point(lat, lon)
        if any(polygon.contains(point) for polygon in control_lines[uuid]):
            non_burnable = 0
            return jsonify(non_burnable), 200

        [value] = raster_dataset.sample([(lon, lat)])

        number = int(value.item())
        # coordinate given out of bounds
        if int(number) not in model_classes['number'].values:
            raise ValueError

        return jsonify(number), 200

    except (KeyError, ValueError): # body not correctly formatted
        return Response(status=400)


@app.route('/control-lines/<float:lat_min>&<float:lat_max>&<float:lon_min>&<float:lon_max>&<string:uuid>', methods=['PUT'])
def put_control_line(lat_min: float, lat_max: float, lon_min: float, lon_max: float, uuid: str) -> Response:
    """
    takes the coordinate values of the rectangular control line
    :param lat_min:
    :param lat_max:
    :param lon_min:
    :param lon_max:
    :param uuid:
    :return: inserts the new polygon into a list of control lines
    """
    try:
        if uuid not in control_lines:
            control_lines[uuid] = []
    except Exception as e:
        return Response(status=500)

    try:
        corners = [(lat_min, lon_min), (lat_min, lon_max), (lat_max, lon_max), (lat_max, lon_min)]
        new_control_line = Polygon(corners)

        control_lines[uuid].append(new_control_line)

        return Response(status=200)
    except:
        return Response(status=500)

@app.route('/control-lines/clear', methods=['GET'])
def clear_control_lines() -> Response:
    """
    Clears the list of control lines
    :return:
    """
    try:
        uuid = str(request.args['uuid'])

        if uuid in control_lines:
            control_lines[uuid].clear()

        return Response(status=200)

    except:
        return Response(status=500)

@app.route('/control-lines/clear-all', methods=['GET'])
def clear_all_control_lines():
    """
    Clears all instances of control lines
    :return:
    """
    try:
        control_lines.clear()
        return Response(status=200)
    except:
        return Response(status=500)


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
    parameters["fuel_load"] = fuel_load

    sav_ratio = model_classes_sav_ratio.loc[model_classes_sav_ratio[parameter_name] == model_number]\
                    .drop(labels=['code', 'number'], axis=1)\
                    .values\
                    .tolist()[0]
    sav_ratio = list(map(float, sav_ratio))
    parameters["sav_ratio"] = sav_ratio

    return jsonify(parameters), 200

