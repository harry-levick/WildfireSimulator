#!/bin/sh
export FLASK_APP=./fuel_model/app.py
export FLASK_ENV=development
source ../venv/bin/activate
flask run -h 0.0.0.0