import json
import unittest
from fuel_model import app


class GetFuelModelTest(unittest.TestCase):
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
        Test the endpoint returns a http 200 when no body
        is sent
        :return:
        """
        res = self.app.get('/model-code')
        self.assertEqual(res.status_code, 200)

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
                    "coordinates": [37.335278, -121.891944]
                },
                "properties": {
                    "location": "San Jose"
                }
            }
        ]
        expected = ['91.0']
        res = self.app.get('/model-code', data=json.dumps(req), content_type='application/json')
        self.assertEqual(res.status_code, 200)

        data = res.data
        self.assertEqual(res.data, expected)

    def test_valid_coordinates_in_bounds(self) -> None:
        """
        Test the endpoint using a list of valid coordinates
        that are all inside the bounds of the state of California.
        :return:
        """
        self.assertEqual(True, False)

    def test_invalid_coordinate(self) -> None:
        """
        Test the endpoint using a coordinate that
        doesnt have valid values for latitude and longitude.
        :return:
        """
        # latitude outside of the bounds -90 <= x <= +90
        # longitude outside of the bounds -180 <= x <= +180
        self.assertEqual(True, False)

    def test_invalid_coordinates(self) -> None:
        """
        Test the endpoint using a list of coordinates that
        dont have valid values for the latitude and longitude.
        :return:
        """
        # latitude outside of the bounds -90 <= x <= +90
        # longitude outside of the bounds -180 <= x <= +180
        self.assertEqual(True, False)

    def test_valid_coordinate_out_of_bounds(self) -> None:
        """
        Test the endpoint using a valid coordinate that are outside
        the region defined by the California state line.
        :return:
        """
        self.assertEqual(True, False)

    def test_valid_coordinates_out_of_bounds(self) -> None:
        """
        Test the endpoint using a list of valid coordinates that
        are outside the region defined by the California state line.
        :return:
        """
        self.assertEqual(True, False)


if __name__ == '__main__':
    unittest.main()
