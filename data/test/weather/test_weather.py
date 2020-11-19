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
        res_data = json.loads(res.data.decode('utf-8'))
        self.assertEqual(200, res.status_code)
        self.assertIn("current", res_data)
        self.assertIn("hourly", res_data)
        self.assertIn("daily", res_data)

    def test_invalid_coordinate(self) -> None:
        """
        Test the endpoint using a coordinate that
        doesnt have valid values for latitude and longitude.
        :return:
        """
        # latitude outside of the bounds -90 <= x <= +90
        # longitude outside of the bounds -180 <= x <= +180
        req = {
            "lat": 100,
            "lon": -190
        }
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(400, res.status_code)

    def test_invalid_body(self) -> None:
        """
        Test the endpoint using an invalid GeoJson string
        :return:
        """
        req = {
            "latitude": 30.916,
            "longitude": -120.023
        }
        res = self.app.get('/weather-data', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 400)


if __name__ == '__main__':
    unittest.main()
