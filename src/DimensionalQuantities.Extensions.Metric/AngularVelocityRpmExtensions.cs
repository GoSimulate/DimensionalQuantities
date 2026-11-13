namespace DimensionalQuantities.Extensions.Metric;

public static class AngularVelocityRpmExtensions
{
	private const double RadiansPerRpm = 2 * Math.PI / 60;

	extension(AngularVelocity)
	{
		public static AngularVelocity FromRpm(double value) => AngularVelocity.FromSiBaseUnit(value * RadiansPerRpm);
	}

	extension(AngularVelocity angularVelocity)
	{
		public double ToRpm() => angularVelocity.AsSiBaseUnit() / RadiansPerRpm;
	}
}
