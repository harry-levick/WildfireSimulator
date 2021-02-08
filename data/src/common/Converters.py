from werkzeug.routing import FloatConverter as BaseFloatConverter


class FloatConverter(BaseFloatConverter):
    regex = r'-?\d+(\.\d+)?'