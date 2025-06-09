from enum import Enum
from typing import List, Optional
import json
from semantic_kernel.functions import kernel_function


class Brightness(str, Enum):
    LOW = "Low"
    MEDIUM = "Medium"
    HIGH = "High"


class LightModel:
    def __init__(self, id: int, name: str, is_on: bool = False, brightness: Optional[Brightness] = None, color: Optional[str] = None):
        self.id = id
        self.name = name
        self.is_on = is_on
        self.brightness = brightness
        self.color = color

    def to_dict(self):
        return {
            "id": self.id,
            "name": self.name,
            "is_on": self.is_on,
            "brightness": self.brightness.value if self.brightness else None,
            "color": self.color
        }


class LightsPlugin:
    """Plugin for controlling lights."""

    def __init__(self):
        # Mock data for the lights
        self.lights = [
            LightModel(id=1, name="Main Stage", is_on=False, brightness=Brightness.MEDIUM, color="#FFFFFF"),
            LightModel(id=2, name="Second Stage", is_on=False, brightness=Brightness.HIGH, color="#FF0000"),
            LightModel(id=3, name="Outside", is_on=False, brightness=Brightness.LOW, color="#FFFF00"),
            LightModel(id=4, name="Entrance", is_on=True, brightness=Brightness.LOW, color="#FFFF00"),
        ]

    @kernel_function(
        description="Gets a list of lights and their current state",
        name="get_lights"
    )
    def get_lights(self) -> str:
        """Gets a list of lights and their current state."""
        return json.dumps([light.to_dict() for light in self.lights])

    @kernel_function(
        description="Changes the state of the light",
        name="change_state"
    )
    def change_state(self, id: int, is_on: bool) -> str:
        """Changes the state of the light."""
        light = next((light for light in self.lights if light.id == id), None)
        if light is None:
            return json.dumps({"error": f"Light with id {id} not found"})

        # Update the light with the new state
        light.is_on = is_on
        return json.dumps(light.to_dict())

    @kernel_function(
        description="Changes the color of the light by passing the RGB color hex code",
        name="change_color"
    )
    def change_color(self, id: int, rgb_color: str) -> str:
        """Changes the color of the light by passing the RGB color hex code."""
        light = next((light for light in self.lights if light.id == id), None)
        if light is None:
            return json.dumps({"error": f"Light with id {id} not found"})

        # Update the light with the new color
        light.color = rgb_color
        return json.dumps(light.to_dict())

    @kernel_function(
        description="Changes the brightness value of the light",
        name="change_brightness"
    )
    def change_brightness(self, id: int, brightness: str) -> str:
        """Changes the brightness value of the light."""
        try:
            brightness_value = Brightness(brightness)
        except ValueError:
            return json.dumps({"error": f"Invalid brightness value: {brightness}. Must be one of: {', '.join([b.value for b in Brightness])}"})

        light = next((light for light in self.lights if light.id == id), None)
        if light is None:
            return json.dumps({"error": f"Light with id {id} not found"})

        # Update the light with the new brightness
        light.brightness = brightness_value
        return json.dumps(light.to_dict())
