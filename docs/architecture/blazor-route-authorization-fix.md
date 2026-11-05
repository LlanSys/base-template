# Blazor deep-link repair

The browser check reproduced a server exception on a direct request to `/control-panel/tenants`: HTTP authorization tries to challenge an authentication service that this browser-token-based UI does not register. In-circuit navigation works because native Blazor policies use the restored API-backed session.

The endpoint change in `src/Frontend/Web/BT.UI.Blazor/Program.cs` is:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode().AllowAnonymous();
```

This permits downloading the initial application shell. Protected component prerendering is disabled; the shell contains no authorized page content. The interactive router restores protected browser storage before rendering `AuthorizeRouteView`. Existing `[Authorize]` page policies, policy-protected layouts, and permission-protected API endpoints remain required. No bearer or refresh token is put into an anonymous response or shared cache. StarterTemplate independently uses this shell/circuit arrangement.

Automatic approval review initially rejected the endpoint change because it considered the anonymous shell a weakening of the HTTP route boundary. The user explicitly approved this documented shell change on 2026-11-04 before implementation. A separately designed HTTP cookie/BFF boundary remains an architectural alternative; it is not introduced by this repair.

Validation completed on 2026-11-04: a direct vendor URL restored the existing vendor session; an authenticated synthetic tenant administrator saw “You do not have permission to open this page” at the same vendor route; an anonymous/expired session reached sign-in; server HTML contained no protected tenant/account content; and the synthetic tenant received HTTP 403 from the vendor API. Both synthetic tenants used for the invitation and route checks were soft-deleted afterward, preserving audit history and returning the list to the original two tenants.
