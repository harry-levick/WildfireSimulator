import requests
from data.src.common.constants import *
from flask import Flask, Response
from flask import jsonify, request

app = Flask(__name__)


@app.route('/weather-data', methods=['GET'])
def get_model_code() -> Response:
    """
    :param lat
    :param lon
    :return: Json data
    """
    args = request.args
    if "lat" not in args or "lon" not in args:
        return Response(status=400)


    current = "current"
    hourly = "hourly"
    daily = "daily"
    wind_speed = "wind_speed"
    wind_degrees = "wind_deg"
    url = WEATHER_API

    params = {
        "lat": args.get("lat"),
        "lon": args.get("lon"),
        "exclude": "alerts,minutely",
        "appid": WEATHER_API_KEY
    }

    res = requests.get(url=url, params=params)
    data = res.json()
    data[current] = {key: data[current][key] for key in data[current].keys() & {wind_speed, wind_degrees}}

    for i, dict in enumerate(data["hourly"]):
        data[hourly][i] = {key: dict[key] for key in data[hourly][i].keys() & {wind_speed, wind_degrees}}

    for i, dict in enumerate(data["daily"]):
        data[daily][i] = {key: dict[key] for key in data[daily][i].keys() & {wind_speed, wind_degrees}}

    return jsonify(data), 200

