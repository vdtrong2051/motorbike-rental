#!/usr/bin/env node
// GD4.6 real-browser acceptance. Runs against a server the user/CI explicitly started.
// Does NOT mutate orders, payments, reviews, or the database. Captures screenshots locally.
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const base = (process.env.GD4_BASE_URL || 'http://127.0.0.1:5119').replace(/\/$/, '');
const fixturePath = process.env.GD4_FIXTURE_PATH || '';
const outputDir = path.resolve(process.env.GD4_OUTPUT || 'artifacts/gd4-live');
const capture = process.env.GD4_CAPTURE !== '0';
const sizes = [
  {name:'mobile-375',width:375,height:812},
  {name:'mobile-390',width:390,height:844},
  {name:'tablet-768',width:768,height:1024},
  {name:'desktop-1440',width:1440,height:900},
];
fs.mkdirSync(outputDir, {recursive:true});
const errors=[]; const checks=[];
let browser;

function record(ok, role, pathname, viewport, message) {
  checks.push({ok,role,path:pathname,viewport,message});
  (ok ? process.stdout : process.stderr).write(`${ok?'PASS':'FAIL'} [${role} ${viewport} ${pathname}] ${message}\n`);
  if (!ok) errors.push(`${role} ${viewport} ${pathname}: ${message}`);
}
async function visit(context, role, pathname, size) {
  const page = await context.newPage();
  page.setViewportSize({width:size.width,height:size.height});
  const runtimeErrors=[];
  page.on('pageerror',e=>runtimeErrors.push(e.message));
  try {
    const response=await page.goto(base+pathname,{waitUntil:'domcontentloaded',timeout:30000});
    const status=response?.status() || 0;
    record(status>=200&&status<400,role,pathname,size.name,`HTTP ${status}`);
    const scan=await page.evaluate(()=>({
      overflow:Math.max(0,document.documentElement.scrollWidth-document.documentElement.clientWidth),
      main:!!document.querySelector('main#main-content'),
      skip:!!document.querySelector('a.gd4-skip'),
      lang:document.documentElement.lang,
      layout:document.documentElement.getAttribute('data-gd4-ui'),
      h1:document.querySelectorAll('h1').length,
      body:document.body.textContent?.slice(0,500) || ''
    }));
    record(scan.overflow<=3,role,pathname,size.name,`No horizontal overflow (${scan.overflow}px)`);
    record(scan.main && scan.skip && scan.lang==='vi',role,pathname,size.name,'Main landmark, skip link, Vietnamese locale');
    record(scan.layout==='workspace' === (role==='owner'||role==='admin'),role,pathname,size.name,`Correct layout: ${scan.layout}`);
    if (pathname==='/' && role==='public') {
      record(await page.locator('input[name="NgayNhanXe"]').count()===1 && await page.locator('input[name="NgayTraXe"]').count()===1,
        role,pathname,size.name,'Home search has rental date range');
    }
    if (capture) {
      const safe=pathname.replace(/[^a-z0-9]/gi,'-').replace(/-+/g,'-').slice(0,64)||'home';
      await page.screenshot({path:path.join(outputDir,`${role}-${size.name}-${safe}.png`),fullPage:true,animations:'disabled'});
    }
    record(runtimeErrors.length===0,role,pathname,size.name,`No uncaught page exceptions (${runtimeErrors.length})`);
  } catch(e) { record(false,role,pathname,size.name,e.message); }
  finally { await page.close(); }
}
async function login(context,role,email,password) {
  const page=await context.newPage();
  try {
    const response=await page.goto(base+'/Account/Login',{waitUntil:'domcontentloaded'});
    if (!response?.ok()) throw Error(`Login page ${response?.status()}`);
    await page.locator('input[name="Email"]').fill(email);
    await page.locator('input[name="Password"]').fill(password);
    await Promise.all([page.waitForURL(url=>!url.pathname.endsWith('/Account/Login'),{timeout:15000}),page.locator('button[type="submit"]').first().click()]);
    record(true,role,'/Account/Login','desktop','Cookie login successful');
  } catch(e) { record(false,role,'/Account/Login','desktop',e.message); throw e; }
  finally { await page.close(); }
}

try {
  const fixture=fixturePath && fs.existsSync(fixturePath) ? JSON.parse(fs.readFileSync(fixturePath,'utf8')) : null;
  if (!fixture || !fixture.email || !fixture.ownerEmail || !fixture.adminEmail || !fixture.password) {
    throw new Error('GD4.6 needs GD4_FIXTURE_PATH JSON with email, ownerEmail, adminEmail, password. Full role acceptance cannot be skipped.');
  }
  browser=await chromium.launch({headless:true});
  const anonymous=await browser.newContext();
  const publicPages=['/','/Vehicle/Index','/Vehicle/Details/'+fixture.vehicleId,'/Account/Login'];
  for (const size of sizes) for(const p of publicPages) await visit(anonymous,'public',p,size);
  await anonymous.close();

  const roleCases=[
    ['customer',fixture.email,['/RentalOrder/MyOrders','/Cart/Index','/RentalOrder/Details/'+fixture.orderId]],
    ['owner',fixture.ownerEmail,['/OwnerPortal/Dashboard','/RentalOrder/OwnerOrders','/OwnerPortal/Calendar','/Vehicle/MyVehicles','/OrderReview/OwnerReviews']],
    ['admin',fixture.adminEmail,['/Admin/Dashboard','/Admin/Users','/Admin/Vehicles','/Admin/Payments','/OrderReview/AdminReviews']],
  ];
  for (const [role,email,pages] of roleCases) {
    const context=await browser.newContext();
    try { await login(context,role,email,fixture.password);
      for(const size of sizes) for(const p of pages) await visit(context,role,p,size);
    } catch { /* login failure already recorded */ }
    await context.close();
  }
} catch(e) { errors.push(String(e)); process.stderr.write(`GD4 LIVE AUDIT ERROR: ${e.message}\n`); }
finally {
  await browser?.close();
  const report={timestampUtc:new Date().toISOString(),base,summary:{checks:checks.length,passed:checks.filter(x=>x.ok).length,failed:errors.length},errors,checks};
  fs.writeFileSync(path.join(outputDir,'report.json'),JSON.stringify(report,null,2));
  process.stdout.write(`GD4.6 summary: ${report.summary.passed}/${checks.length} assertions, ${errors.length} failures. ${outputDir}\n`);
}
if(errors.length)process.exit(1);
