import json
import unittest
from data.src.fuel_model import app


class TestGetFuelModelParameters(unittest.TestCase):
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

    def test_no_param(self) -> None:
        """
        Test the endpoint returns a http 400 when no
        query parameter is sent
        :return:
        """
        res = self.app.get('/model-parameters')
        self.assertEqual(400, res.status_code)

    def test_valid_model_code(self) -> None:
        """
        Test the endpoint using a single
        valid fuel model code.
        :return:
        """
        model_code = 101
        expected = {
            "code": "GR1",
            "number": 101,
            "name": "Short, sparse, dry climate grass",
            "description": "Short, sparse dry climate grass is short, naturally or heavy grazing, "
                           "predicted rate of fire spread and flame length low.",
            "fuel_load": [0.100, 0.00, 0.00, 0.3, 0.00],
            "type": "Dynamic",
            "sav_ratio": [2200, 2000, 9999],
            "fuel_bed_depth": 0.4,
            "dead_fuel_moisture_of_extinction": 0.15,
            "characteristic_sav": 2054,
            "bulk_density": 0.05,
            "relative_packing_ratio": 0.22
        }
        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_valid_non_burnable_model_code(self) -> None:
        """
        Test the endpoint using a single
        valid fuel model code.
        :return:
        """
        model_code = 93

        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(200, res.status_code)

    def test_zero_model_code(self) -> None:
        """
        Test the endpoint using a single
        valid fuel model code.
        :return:
        """
        model_code = 0

        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(200, res.status_code)

    def test_invalid_model_code(self) -> None:
        """
        Test the endpoint using a single
        invalid fuel model code.
        :return:
        """
        model_code = 90
        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(400, res.status_code)

    def test_valid_model_code_from_valid_coordinate(self) -> None:
        """
        Test the model-number endpoint using a single valid coordinate
        that is inside the bounds of the state of California. Then
        use the returned model to get the parameters
        :return:
        """
        expected_model_number = 182
        expected_model_params = {
            "code": "TL2",
            "number": 182,
            "name": "Low broadleaf litter",
            "description": "Low load broadleaf litter, broadleaf, hardwood litter, spread rate and flame low.",
            "fuel_load": [1.40, 2.30, 2.20, 0.00, 0.00],
            "type": "Static",
            "sav_ratio": [2000, 9999, 9999],
            "fuel_bed_depth": 0.2,
            "dead_fuel_moisture_of_extinction": 0.25,
            "characteristic_sav": 1806,
            "bulk_density": 1.35,
            "relative_packing_ratio": 5.87
        }
        res = self.app.get('/model-number', query_string={"lat": 37.826194, "lon": -122.420930})
        self.assertEqual(res.status_code, 200)
        model_number = json.loads(res.data.decode('utf-8'))
        self.assertEqual(expected_model_number, model_number)

        res = self.app.get('/model-parameters', query_string={"number": model_number})
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected_model_params, json.loads(res.data.decode('utf-8')))


if __name__ == '__main__':
    unittest.main()
