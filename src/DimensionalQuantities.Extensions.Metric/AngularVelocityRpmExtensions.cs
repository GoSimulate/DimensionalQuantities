namespace DimensionalQuantities.Extensions.Metric;

/// <summary>Converts angular velocity between radians per second and revolutions per minute.</summary>
public static class AngularVelocityRpmExtensions
{
	private const double RadiansPerRpm = 2 * Math.PI / 60;

	extension(AngularVelocity)
	{
		/// <summary>Creates an angular velocity in radians per second from revolutions per minute.</summary>
		public static AngularVelocity FromRpm(double value) => AngularVelocity.FromSiBaseUnit(value * RadiansPerRpm);
	}

	extension(AngularVelocity angularVelocity)
	{
		/// <summary>Converts the stored radians per second to revolutions per minute.</summary>
		public double ToRpm() => angularVelocity.AsSiBaseUnit() / RadiansPerRpm;
	}
}
