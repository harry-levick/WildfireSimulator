import json
import unittest
from data.src.fuel_moisture import app


class TestGetLiveFuelMoisture(unittest.TestCase):
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
        res = self.app.get('/live-fuel-moisture-content')
        self.assertEqual(res.status_code, 400)

    def test_valid_coordinate_in_bounds(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        expected = 50
        res = self.app.get('/live-fuel-moisture-content', query_string={"lat": 34.916, "lon": -120.023})
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
        res = self.app.get('/live-fuel-moisture-content', query_string={"lat": 100, "lon": -190})
        self.assertEqual(400, res.status_code)

    def test_valid_coordinate_out_of_bounds(self) -> None:
        """
        Test the endpoint using a valid coordinate that are outside
        the region defined by the California state line.
        :return:
        """
        expected = 0
        res = self.app.get('/live-fuel-moisture-content', query_string={"lon": 40.730610, "lat": -73.935242})
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_invalid_body(self) -> None:
        """
        Test the endpoint using an invalid GeoJson string
        :return:
        """
        res = self.app.get('/live-fuel-moisture-content')
        self.assertEqual(res.status_code, 400)


if __name__ == '__main__':
    unittest.main()
