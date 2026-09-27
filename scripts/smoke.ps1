param([string]$BaseUrl = "http://localhost:5057", [string]$Email = "test@example.com", [string]$Password = "Test1234!")
$ErrorActionPreference = 'Stop'
$health = Invoke-WebRequest "$BaseUrl/health" -UseBasicParsing
if ($health.StatusCode -ne 200 -or $health.Content -notmatch 'Healthy') { throw 'health check failed' }
$login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/auth/login" -ContentType 'application/json' -Body (@{email=$Email;password=$Password} | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$dashboard = Invoke-RestMethod -Uri "$BaseUrl/api/v1/dashboard" -Headers $headers
$placement = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/placement/sessions" -Headers $headers -ContentType 'application/json' -Body '{"questionCount":8}'
$community = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/community/submissions" -Headers $headers -ContentType 'application/json' -Body '{"type":"writing","content":"Smoke test submission","prompt":"smoke"}'
$secureHeaders = @('X-Content-Type-Options','X-Frame-Options','Referrer-Policy','X-Correlation-ID')
foreach ($name in $secureHeaders) { if (-not $health.Headers[$name] -and $name -ne 'X-Correlation-ID') { throw "missing security header: $name" } }
[pscustomobject]@{ Health = $health.StatusCode; CoachRoute = $dashboard.coachRoute; PlacementQuestions = $placement.questionCount; CommunityStatus = $community.status; CorrelationId = $health.Headers['X-Correlation-ID'] } | ConvertTo-Json
