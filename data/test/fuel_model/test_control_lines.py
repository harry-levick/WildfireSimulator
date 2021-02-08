import json
import unittest
from data.src.fuel_model import app


class TestControlLines(unittest.TestCase):
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
        self.test_clear_control_lines()

    def test_control_line_returns_not_burnable_when_inside_polygon(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        expected = 0
        self.test_put_control_line(37.826193, 37.827, -122.420940, -122.0)
        self.test_model_code_with_known_coords(expected)

    def test_control_line_returns_burnable_when_on_perimeter(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        self.test_put_control_line(37.826194, 37.827, -122.420930, -122.0)
        self.test_model_code_with_known_coords()

    def test_control_line_returns_burnable_when_outside_polygon(self) -> None:
        """
        Test the endpoint using a single valid coordinate
        that is inside the bounds of the state of California.
        :return:
        """
        self.test_put_control_line(37.826195, 37.827, -122.420920, -122.0)
        self.test_model_code_with_known_coords()

    def test_clear_control_lines(self) -> None:
        """

        :return:
        """
        res = self.app.get('/control-lines/clear')
        self.assertEqual(res.status_code, 200)

    def test_put_control_line(self, lat_min=1, lat_max=2, lon_min=1, lon_max=2) -> None:
        """
        Test the put control line endpoint
        :param lat_min:
        :param lat_max:
        :param lon_min:
        :param lon_max:
        :return:
        """
        polygon_vertices = f"{lat_min}&{lat_max}&{lon_min}&{lon_max}"
        res = self.app.put(f'/control-lines/{polygon_vertices}')
        self.assertEqual(res.status_code, 200)

    def test_model_code_with_known_coords(self, expected=182) -> None:
        """

        :param expected:
        :return:
        """
        res = self.app.get('/model-number', query_string={"lat": 37.826194, "lon": -122.420930})
        self.assertEqual(res.status_code, 200)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))
