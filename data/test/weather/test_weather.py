import json
import unittest
from src.weather import app

class TestGetWeather(unittest.TestCase):
    def setUp(self) -> None:
        """
        Executed prior to each unit test.
        :return:
        """
        self.app = app.app.test_client()
        self.app.testing = True

    def tearDown(self) -> None:
        """
        Executed after each test.
        :return:
        """
        pass

    def test_no_params(self) -> None:
        """
        Test the endpoint returns a http 400 when no body
        is sent
        :return:
        """
        res = self.app.get('/weather-data')
        self.assertEqual(res.status_code, 400)

    def test_valid_coordinate_in_bounds(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        req = {
            "lat": 34.916,
            "lon": -120.023
        }
        res = self.app.get('/weather-data', query_string=req, content_type='application/json')
        self.assertEqual(200, res.status_code)

    def test_valid_coordinates_in_bounds(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [34.916, -120.023]
                },
                "properties": {}
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [36.2567, -117.8710]
                },
                "properties": {}
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [34.09457, -118.86832]
                },
                "properties": {}
            }
        ]
        expected = [50, 73, 49]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 200)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_invalid_coordinate(self) -> None:
        """
        Test the endpoint using a coordinate that
        doesnt have valid values for latitude and longitude.
        :return:
        """
        # latitude outside of the bounds -90 <= x <= +90
        # longitude outside of the bounds -180 <= x <= +180
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [100, -190]
                },
                "properties": {}
            }
        ]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(400, res.status_code)

    def test_invalid_coordinates(self) -> None:
        """
        Test the endpoint using a list of coordinates that
        dont have valid values for the latitude and longitude.
        :return:
        """
        # latitude outside of the bounds -90 <= x <= +90
        # longitude outside of the bounds -180 <= x <= +180
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [100, -190]
                },
                "properties": {}
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [200, -200]
                },
                "properties": {}
            }
        ]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(400, res.status_code)

    def test_valid_coordinate_out_of_bounds(self) -> None:
        """
        Test the endpoint using a valid coordinate that are outside
        the region defined by the California state line.
        :return:
        """
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [40.730610, -73.935242]
                },
                "properties": {
                    "name": "New York City"
                }
            }
        ]
        expected = [0]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_valid_coordinates_out_of_bounds(self) -> None:
        """
        Test the endpoint using a list of valid coordinates that
        are outside the region defined by the California state line.
        :return:
        """
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [40.730610, -73.935242]
                },
                "properties": {
                    "name": "New York City"
                }
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [51.509865, -0.118092]
                },
                "properties": {
                    "name": "London"
                }
            }
        ]
        expected = [0, 0]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_invalid_body(self) -> None:
        """
        Test the endpoint using an invalid GeoJson string
        :return:
        """
        req = [
            {
                "type": "Feature",
                "properties": {}
            }
        ]
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 400)


if __name__ == '__main__':
    unittest.main()
