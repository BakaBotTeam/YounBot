using System;

namespace YounBot.Utils.Hypixel;

public class ApiFailedException(string message) : Exception(message);