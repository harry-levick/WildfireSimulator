import json
import unittest
from src.fuel_model import app

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
            "fuel load": [0.100, 0.00, 0.00, 0.3, 0.00],
            "type": "Dynamic",
            "sav ratio": [2200, 2000, 9999],
            "fuel bed depth": 0.4,
            "dead fuel moisture of extinction": 0.15,
            "characteristic sav": 2054,
            "bulk density": 0.05,
            "relative packing ratio": 0.22
        }
        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(200, res.status_code)
        self.assertEqual(expected, json.loads(res.data.decode('utf-8')))

    def test_invalid_model_code(self) -> None:
        """
        Test the endpoint using a single
        invalid fuel model code.
        :return:
        """
        model_code = 90
        res = self.app.get('/model-parameters', query_string={"number": model_code})
        self.assertEqual(400, res.status_code)


if __name__ == '__main__':
    unittest.main()
