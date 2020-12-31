import requests
from data.src.common.constants import *
from flask import Flask, Response
from flask import jsonify, request

CURRENT = "current"
HOURLY = "hourly"
DAILY = "daily"
WIND_SPEED = "wind_speed"
WIND_DEGREES = "wind_deg"
app = Flask(__name__)

def metres_per_sec_to_feet_per_min(x: int) -> int:
    return x * METRES_PER_SECOND_TO_FEET_PER_MINUTE_CONVERSION

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


    url = WEATHER_API

    params = {
        "lat": args.get("lat"),
        "lon": args.get("lon"),
        "exclude": "alerts,minutely",
        "appid": WEATHER_API_KEY
    }

    res = requests.get(url=url, params=params)
    data = res.json()

    data[CURRENT] = {key: data[CURRENT][key] for key in data[CURRENT].keys() & {WIND_SPEED, WIND_DEGREES}}
    data[CURRENT][WIND_SPEED] = metres_per_sec_to_feet_per_min(data[CURRENT][WIND_SPEED])

    for i, dict in enumerate(data["hourly"]):
        data[HOURLY][i] = {key: dict[key] for key in data[HOURLY][i].keys() & {WIND_SPEED, WIND_DEGREES}}
        data[HOURLY][i][WIND_SPEED] = metres_per_sec_to_feet_per_min(data[HOURLY][i][WIND_SPEED])

    for i, dict in enumerate(data["daily"]):
        data[DAILY][i] = {key: dict[key] for key in data[DAILY][i].keys() & {WIND_SPEED, WIND_DEGREES}}
        data[DAILY][i][WIND_SPEED] = metres_per_sec_to_feet_per_min(data[DAILY][i][WIND_SPEED])


    return jsonify(data), 200

