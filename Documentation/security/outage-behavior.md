# Outage behavior

What Identity keeps doing, and what it refuses, when a store or dependency is down. The contract is **fail closed**: a credential is never honored on the strength of a store that cannot answer. Related: [Cratis/Identity #121](https://github.com/Cratis/Identity/issues/121), [#57](https://github.com/Cratis/Identity/issues/57).

Rows marked **open** have no spec yet; treat them as intent, not proof.

| Outage | Keeps working | Refused | Recovery and evidence |
| --- | --- | --- | --- |
| Chronicle kernel down | Revocation is still acknowledged (HTTP 200). | Online refresh is denied as `invalid_grant` (HTTP 400). | Specs: `Source/Identity.Integration.Specs/for_Revocation/when_chronicle_is_unavailable.cs` (CI release gate "Release gate - Chronicle-down behaviour"); recovery delivery retry: `Source/Identity.Specs/Revocation/when_both_stores_are_restored_and_chronicle_is_unavailable.cs`. The spec restarts Chronicle and waits for recovery afterwards. |
| Read model behind or stalled (Chronicle up) | Everything not waiting on the projection. | Sign-in is refused as `temporarily_unavailable` once the catch-up budget (`Accounts.ReadModelCatchUpTimeout`, default 30 s) elapses (`Source/Identity/Protocol/ReadModelCatchUp.cs`, `EventSourcedAccountAdapter.SignIn`). | Specs: `Source/Identity.Specs/Protocol/for_ReadModelCatchUp/when_the_projection_times_out.cs`, `for_ProtocolCommands/when_a_command_succeeds_without_appends_while_a_projection_is_stalled.cs`. |
| Request abandoned during catch-up | Other requests. | The abandoned sign-in stops waiting: the HTTP request's `RequestAborted` token flows from `GitHubSignInEvents` through `IAccountSignIn`, `ProtocolCommands` and `SignInWithUpstreamKey` into the wait, and cancellation is not reported as `temporarily_unavailable`. | Specs: `Accounts/SigningIn/when_signing_in/and_the_request_is_cancelled_during_the_catch_up_wait.cs`, `Protocol/for_ProtocolCommands/when_a_command_succeeds_with_an_active_request.cs`. |
| Fence log unavailable | Nothing that needs a fence decision. | Redemption and issuance are denied: `RefuseIssuanceDuringRecovery` closes the restriction gate and returns false when the recovery epoch cannot be read (`Source/Identity/Authorization/RefuseIssuanceDuringRecovery.cs`). | **Open:** no end-to-end outage spec. Restore and completeness behavior: `Source/Identity.Specs/Revocation/when_a_backup_omits_denials_above_its_checkpoint.cs`. |
| Operational store (Mongo) down | Unexpired signed tokens validate offline at resources. | Not verified against code. | **Open:** no spec stops the operational store; no claim is made beyond fail closed intent. |
| Upstream provider (GitHub) down | Existing sessions. | A failed provider call (HTTP error, malformed response) ends the sign-in as `access_denied`; `temporarily_unavailable` is reserved for an account-store refusal (`Source/Identity/Upstream/GitHubSignInEvents.cs`, `RemoteFailure`). | **Open:** no spec for a provider outage end to end. |
| Identity restart or restore | Resources validating unexpired signed tokens. | While the restriction gate is closed the guarded issuance paths refuse (health check `identity-restriction-recovery`, registered in `Source/Identity/Hosting/IdentityServiceCollectionExtensions.cs`). | The integration host waits for recovery (`IdentityHost.WaitForRecovery`). |

## Notes

- Offline JWT validation at a resource continues until token expiry during any Identity outage; see [Resource token validation](resource-token-validation.md) for the revocation window.
- Recovery after compromise follows the runbooks in [Compromise matrix and recovery runbooks](compromise-matrix-v1.md).
- Making the Chronicle-down gate a required check is a repository setting (list the `Build and specs` job as a required status check on `main`), not a file in this repository.
