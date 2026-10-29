import { Typography } from 'antd'
import { applicationName, gitHubRepositoryIssues } from '../../constants'

export default function PrivacyPolicyPage() {
  return (
    <Typography>
      <h1>Privacy Policy</h1>
      <p>Updated October 28, 2026.</p>
      <p>
        {applicationName} processes opened saves and source profiles in your browser. The editor does not
        upload these files to a save-processing server. Edited saves and Pokémon archives are downloaded
        to your device. Preferences and installed plug-in information use browser storage; imported
        source profiles currently last only for the browser session.
      </p>
      <p>
        This fork does not start Google Analytics, advertising scripts or error-reporting telemetry.
        GitHub Pages delivers the site and can receive normal hosting request information. Pokémon
        artwork and optional plug-in catalogs or downloads can request resources from third-party
        hosts, which receive connection information and the requested resource addresses.
      </p>
      <p>
        Optional plug-ins run in the editor and may access loaded game data. Their behavior depends
        on their author. Opening an external battle calculator can put exported team text in that
        calculator’s address. External sites and installed plug-ins have their own data practices.
      </p>
      <p>
        Questions or corrections can be raised on <a href={gitHubRepositoryIssues}>GitHub</a>.
        Do not attach private saves to a public issue unless you intend to publish them.
      </p>
    </Typography>
  )
}
