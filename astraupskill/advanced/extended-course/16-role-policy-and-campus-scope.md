# 16. Role policy and campus scope

CampusOps models both roles and campus access, but they are enforced at specific boundaries rather than automatically across every endpoint. The demo authentication handler also trusts caller-supplied headers for teaching purposes. This chapter builds a policy matrix from actual mappings, separates role from resource scope, and defines a stronger identity proposal without overstating the existing tests.

Read [DemoAuthentication and CampusAuthorizer](../../../src/CampusOps.Api/Security/DemoAuthentication.cs), [Program](../../../src/CampusOps.Api/Program.cs), [location endpoints](../../../src/CampusOps.Api/Locations/LocationEndpoints.cs), [technician endpoints](../../../src/CampusOps.Api/Technicians/TechnicianEndpoints.cs), and [authorization tests](../../../tests/CampusOps.Api.Tests/Security/AuthorizationTests.cs). Use invented campus names and local fixtures. No real identity provider or credential is needed for the exercises.

## The demo scheme creates a principal from headers

The handler requires both X-Demo-Role and X-Demo-Campus. If either is missing, it returns no authentication result. It accepts Technician, Dispatcher, or Manager as role values, then creates claims for demo-user, the supplied role, and the supplied campus. It does not validate a signed token, password, external issuer, or a campus membership directory.

This is useful scaffolding for testing how authorization policies behave with different claims. It is not evidence that a remote caller is a real manager or belongs to the named campus. A production trust boundary must establish those claims from verified identity evidence rather than accepting the same freely chosen headers.

The distinction should remain visible in documentation and tests. A test using Manager headers can prove that a Manager principal is permitted by a route under the teaching host. It cannot prove resistance to a hostile caller choosing Manager. The scheme's name and purpose should not be hidden behind generic language such as secure authentication implemented.

## Named policies need explicit application

Program defines Operations as requiring Dispatcher or Manager and Manager as requiring Manager. Parts routes and work-order routes use Operations. Procurement and webhook groups use Manager. Location creation uses Manager and an additional campus check. These are concrete source observations about the mapped endpoints.

Other inspected mappings, including asset operations, maintenance request operations, technician creation and assignment, and service-level endpoints, do not attach those named authorization requirements in their files. Program does not define a fallback authorization policy in the inspected composition. A named policy existing in the service collection does not protect routes that never request it.

A full route review should enumerate actual mappings rather than infer protection from URL prefixes. Assignment is mapped directly under a work-orders-shaped path in TechnicianEndpoints, outside the authorized group in WorkOrderEndpoints. Metadata belongs to the builder path used to create the endpoint, not to matching words in its URL.

## CampusAuthorizer checks one supplied relationship

EnsureAccess reads the principal's campus claim and compares it case-insensitively with the supplied campus string. It throws security.campus_forbidden on mismatch. Location creation invokes it against request.Campus before creating the location. That is a real resource-scope check for that operation under the demo identity model.

The helper does not automatically filter lists, load a resource's campus, or protect every work order associated with a location. A service taking only a resource GUID needs to resolve the resource-to-campus relationship before it can apply an equivalent rule. Merely injecting CampusAuthorizer into the container does not perform those lookups.

The comparison ignores case but does not normalize surrounding whitespace in the helper. Domain Location later trims the campus value. If normalization policy matters, define it consistently before authorization and storage rather than allowing the two boundaries to interpret slightly different strings. Tests should cover the intended canonical representation.

## Worked cross-campus scenario

A Manager principal has campus North. Creating a location whose request campus is South reaches EnsureAccess and rejects. Creating North can proceed if other fields are valid. The domain exception handler currently maps that campus rule to status 400, which differs from a route-policy forbidden response. The distinction belongs in the API contract review.

Now consider reading a location list. The inspected GET mapping does not call CampusAuthorizer or require a named policy, and LocationService returns the store's collection. It is therefore unsupported to claim that all location reads are campus-filtered based solely on the create check. A proposed privacy policy must decide whether location catalog data is public, role-limited, or campus-limited and enforce that choice on reads.

Similarly, a work-order ID does not carry a trusted campus claim by itself. A proposed operations command can resolve the order's LocationId, load the location, and compare verified actor scope. That relationship may change if assets move, so the policy should specify whether work-order location is historical or current. Authorization depends on the product's resource model, not just middleware.

## What the existing tests prove

The authorization tests check that unauthenticated procurement vendor creation returns Unauthorized and that a Dispatcher principal returns Forbidden. They target one Manager-only group and establish useful negative cases. They do not verify Manager success, campus mismatch behavior, every write route, or production identity verification.

The request body is absent in those negative tests, which is useful for checking that the authorization gate rejects before business creation can succeed. A positive test needs a valid body and should inspect the resulting state, not merely a successful status. A scope test needs a resource relationship and claims that differ deliberately.

Keep inspected tests separate from executed tests in your evidence report. This workbook's bounded console labs do not start the API host. A proposed request matrix can still be thorough and valuable, but its expected responses should not be reported as runtime observations until the tests actually run.

## Trusted actor and supplied attribution

Comment author and procurement approver are supplied strings in current request models. Route authorization can restrict who submits them, but it does not guarantee those strings identify the authenticated caller. A proposed audit record should store verified subject identity separately from display text or on-behalf-of attribution.

The same principle applies to campus scope. A request-body Campus field identifies the requested target, while the principal's campus claim represents the actor's allowed scope under the chosen trust model. The server compares them; it should not appoint the request's target value as the actor's authority. Keep those input roles separate in DTO and service design.

A production actor context can include subject ID, roles or permissions, campus memberships, and relevant delegation claims. Define how it is created and validated, and pass only the needed information into application commands. Avoid making every domain object depend directly on an HTTP principal when a clear application boundary can enforce the policy.

## Proposed policy coverage without overreach

Start by deciding the intended access policy for each operation. Some catalog reads may intentionally be public. Some request submissions may be open to campus users. Inventory reservation may require operations staff, while approval requires a manager. The task is not to attach the strongest role everywhere, which could remove legitimate features; it is to make policy deliberate and consistent.

For each protected command, specify role, resource scope, and any delegation rule separately. Then write a matrix covering anonymous, valid wrong-role, valid right-role wrong-campus, and valid right-role right-campus cases. Add missing-resource behavior and consider whether the API should reveal resource existence before rejecting scope.

Do not introduce hypothetical warnings unrelated to the source. The concrete gaps here are enough for a useful review: demo claims are not verified production identity, policy coverage varies by mapping, campus checks are localized, and supplied attribution is not actor proof. A focused proposal can address those exact boundaries without redesigning the entire application.

## Independent exercises

**C16.1: Route matrix.** Map the inspected write endpoints to current metadata and proposed intended policy. Include separately mapped assignment and service-level actions. State which observations require a runtime request test rather than a source reading alone.

**C16.2: Campus authority.** Design a work-order action boundary that derives campus from the resource and compares it with verified actor membership. Include missing location, changed location policy, and a cross-campus manager case.

**C16.3: Identity migration.** Replace the demo scheme in a design with a verified identity source while preserving deterministic tests through a dedicated test scheme. Explain which existing tests remain useful and which new evidence establishes claim trust and actor attribution.

## Review checkpoint

Include service-to-service callers in the proposed policy model. A preventive worker may need authority to create scheduled requests without impersonating a human manager. Its identity should carry a bounded system permission and preserve the plan or occurrence that justified the action. Granting every background component an unrestricted manager role makes audit interpretation and least-scope reasoning harder. The current worker bypasses HTTP by invoking services directly, so endpoint metadata alone cannot govern that path. A complete application policy identifies which trusted internal commands are allowed and how their authority is recorded.

Your authorization argument should name who the actor is, why that identity is trusted, which operation is allowed, and which resource scope it covers. A role header, policy registration, URL prefix, and request-supplied name each answer less than a casual reading may suggest. Precise boundaries make a stronger design possible.
