using System;
using System.Web;
using System.Web.UI;
using System.Data;
using System.Data.SqlClient; 
using System.Web.Configuration;
using Oracle.DataAccess.Client;

namespace VideologLookupWebApp
{
	
	public partial class Default : System.Web.UI.Page
	{
		
		private string passedLrsKey = HttpContext.Current.Request.QueryString["lrsKey"];
		private string passedOffset = HttpContext.Current.Request.QueryString["offset"];
		private string lrsKey = "";
		private double offset;
		private string reducedPath = "";
		private string connString = "";
		private OracleConnection oraCon;
		private OracleConnection oraCon2;

		public string readConn()
		{
			
			System.Configuration.Configuration rootWebConfig =
				System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/VideologLookupWebApp");
			System.Configuration.ConnectionStringSettings connStringContainer;
			int connStringCount = rootWebConfig.ConnectionStrings.ConnectionStrings.Count;
			if (connStringCount > 0) {
				connStringContainer = rootWebConfig.ConnectionStrings.ConnectionStrings ["oracleDBConnString"];
			}
			else {
				connStringContainer = null;
			}
			if (connStringContainer != null) {
				connString = connStringContainer.ConnectionString;
			} else {
				connString = null;
			}

			return connString;
		}

		public string StartConn ()
		{
			lrsKey = Convert.ToString (passedLrsKey);

			if (lrsKey == null) {
				lrsKey = "083K0000400S0";
			}

			if (passedOffset == null) {
				offset = 20.004;
			} else {
				offset = Convert.ToDouble (passedOffset);
			}

			reducedPath = "";
			string districtString = "";
			string sessionString = "";
			string firstPortion; // Build this from table data.
			string secondPortion; // Build this from table data.
			string calcFrame;
			string thirdPortion; // Build this from table data.
			string fourthPortion;// Build this from table data.
			string connectionString = readConn ();
			string outputForDebug = "";
			int lowFrame = 0;
			int highFrame = 0;
			int frameDifference = 0;
			double lowOffset = 0.0;
			double highOffset = 0.0;
			double offsetDifference = 0.0;
			double frameIncrement = 0.0;

			// Overview ------------------------------------------------------
			// Store the highest and lowest OFFSET found for the LRSKEY.
			// Store the highest and lowest FRAME found for the LRSKEY.

			// Subtract the lowest OFFSET found from the highest OFFSET found.
			// Subtract the lowest FRAME found from the highest FRAME found.

			// Divide the FRAME_Result by the OFFSET_Result.
			// Store the OFFSET_Per_FRAME value.

			// Take the offset passed in and multiply by OFFSET_Per_FRAME.
			// Take the Math.Floor of the multiplication result and
			// use it PLUS the lowest FRAME found as the Frame value for
			// the route location.
			// -----------------------------------------------------------------

			try
			{
				// Build the oracle command for the ODO_START.
				string queryString = "SELECT DISTRICT, SESSION_NAME, FRAME, OFFSET " + // Have to get all of the relevant fields.
					"FROM VIDEOLOG.ROUTESVIEW " +					// Use the ROUTESVIEW table (no underscore) to get the latest cycle only.
					"WHERE LRS_KEY = '" + lrsKey + "' AND SIGN = '+' AND TAG LIKE '%Started Odometer%' "; // Only interested in the '+' sign route (primary dir).
				// Place the selection criteria here, from the passed in values.
				// Well, only the lrsKey gets a decent match in SQL, have to use minimum difference between doubles
				// below in order to find a near match for the offset.

				// Get the oracle connection string
				string connString = readConn ();

				// Open the oracle connection
				oraCon = new OracleConnection (connString);
				oraCon.Open ();

				// Create the oracle command
				OracleCommand oraCommand = new OracleCommand (queryString, oraCon);
				// Create a reader object with the oracle command
				OracleDataReader reader = oraCommand.ExecuteReader ();
				// start the
				// while (reader.read()){ loop
				while (reader.Read ()) {
					
					districtString = reader ["DISTRICT"].ToString();
					sessionString = reader ["SESSION_NAME"].ToString();
					lowFrame = Convert.ToInt32 (reader ["FRAME"]);
					lowOffset = Convert.ToDouble (reader ["OFFSET"]);

				}
			}

			finally {
				oraCon.Close ();
				oraCon.Dispose ();
			}


			try
			{

				// Build the oracle command for the ODO_STOP.
				string queryString2 = "SELECT DISTRICT, SESSION_NAME, FRAME, OFFSET " + // Have to get all of the relevant fields.
					"FROM VIDEOLOG.ROUTESVIEW " +					// Use the ROUTESVIEW table (no underscore) to get the latest cycle only.
					"WHERE LRS_KEY = '" + lrsKey + "' AND SIGN = '+' AND TAG LIKE '%Stopped Odometer%'"; // Only interested in the '+' sign route (primary dir).
				// Place the selection criteria here, from the passed in values.
				// Well, only the lrsKey gets a decent match in SQL, have to use minimum difference between doubles
				// below in order to find a near match for the offset.

				// Get the oracle connection string
				string connString2 = readConn ();

				// Open the oracle connection
				oraCon2 = new OracleConnection (connString2);
				oraCon2.Open ();

				// Create the oracle command
				OracleCommand oraCommand2 = new OracleCommand (queryString2, oraCon2);
				// Create a reader object with the oracle command
				OracleDataReader reader2 = oraCommand2.ExecuteReader ();
				// start the
				// while (reader.read()){ loop
				while (reader2.Read ()) {

					// Have already set Session and District.
					// Continue to high frame and high offset.
					highFrame = Convert.ToInt32 (reader2 ["FRAME"]);
					highOffset = Convert.ToDouble (reader2 ["OFFSET"]);

				}

			}

			// Make sure that the connection gets closed no matter what happens in the try block.
			finally
			{
				oraCon2.Close ();
				oraCon2.Dispose ();
			}

			frameDifference = highFrame - lowFrame;
			offsetDifference = highOffset - lowOffset;
			frameIncrement =  frameDifference / offsetDifference;
			calcFrame = (Math.Round(offset * frameIncrement) + lowFrame).ToString();

			firstPortion = districtString;
			secondPortion = sessionString;
			// Problem in fourthPortion. Seems to have been fixed by using single quotes to
			// delcare that 'X' is a character. "X" is not, might be a string instead.
			fourthPortion = calcFrame.PadLeft(5, '0');
			thirdPortion = fourthPortion.Substring (0, 3);

			if (frameIncrement <= 0.0001)
			{
				outputForDebug = "_FrameIncrementIs_0"; // Add more info here later if this is a recurring problem.
				return outputForDebug;
			}

			reducedPath = "http://videolog.ksdot.org/images" + firstPortion + "/" + secondPortion + "/front/dir_" + thirdPortion + "/F_" + fourthPortion + ".jpg";
			//reducedPath = "http://videolog.ksdot.org/images" + offset.ToString() + "dd_dd" + highFrame.ToString() + "-" + lowFrame.ToString()+ "ee_ee" + frameIncrement.ToString() + "ff_ff" + firstPortion + "/" + secondPortion + "/front/dir_" + thirdPortion + "/F_" + fourthPortion + ".jpg";
			//reducedPath = "http://videolog.ksdot.org/images" + firstPortion + "/" + secondPortion + "/front/dir_" + thirdPortion + "/F_" + fourthPortion + ".jpg";

			return reducedPath;

		// Create a 2nd table and a 2ndary select/view if the first one comes up null (incase the info in one table
		// is being accessed at the same time that the first table is deleted/being rebuilt.
		// Make the script that deletes/updates the two tables delete the SECOND table first
		// so that if the first table is deleted/being rebuilt, the SECOND table has already
		// been rebuilt and there will be no chance for it to have started being deleted/rebuilt
		// when the script tries to fail-over to it.
		// ROUTES_VIEW and ROUTES_VIEW_2ND -- table names to use.
		// Check *_2ND first, if it does not exist in that table, check the original.
		// If it doesn't exist in either... sorry, it just doesn't exist.

		// Or, maybe I have to access this in the native Oracle because I can't get it
		// out properly otherwise.
		// That would not be particularly optimal, but it might be necessary.
		// Try using cx_Oracle to get a count of the rows, I guess.
		
		//Use the connectionstring from the web.config file.


		}
	}
}