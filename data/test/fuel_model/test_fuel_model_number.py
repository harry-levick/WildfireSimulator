import json
import unittest
from src.fuel_model import app


class TestGetFuelModelNumber(unittest.TestCase):
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

    def test_no_body(self) -> None:
        """
        Test the endpoint returns a http 400 when no body
        is sent
        :return:
        """
        res = self.app.get('/model-code')
        self.assertEqual(res.status_code, 400)

    def test_valid_coordinate_in_bounds(self) -> None:
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
                    "coordinates": [37.826194, -122.420930]
                },
                "properties": {}
            }
        ]
        expected = [182.0]
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 200)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_valid_coordinates_in_bounds(self) -> None:
        """
        Test the endpoint using a list of valid coordinates
        that are all inside the bounds of the state of California.
        :return:
        """
        req = [
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [37.9325133, -122.5960289]
                },
                "properties": {}
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [35.119335, -117.797578]
                },
                "properties": {}
            },
            {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [34.737776, -120.414441]
                },
                "properties": {}
            }
        ]
        expected = [165.0, 101.0, 122.0]
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
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
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
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
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
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
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
        self.assertEqual(400, res.status_code)

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
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
        self.assertEqual(400, res.status_code)

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
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 400)


if __name__ == '__main__':
    unittest.main()
