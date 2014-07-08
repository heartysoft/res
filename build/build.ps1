$framework = '4.0'
Include .\version.ps1

properties {
    $config= if($config -eq $null) {'Debug' } else {$config}
    $base_dir = resolve-path .\..
    $source_dir = "$base_dir\src"
    $tools_dir = "$base_dir\tools"
    $env = "local"
    $out_dir = "$base_dir\out\$config"
    $res_dir = "$source_dir\res"
    $res_artefacts_dir="$res_dir\Res\bin\$config"
    $resclient_artefacts_dir="$res_dir\Res.Client\bin\$config"
    $res_test_dir = "$res_dir\Res.Core.Tests\bin\$config"
    $test_results_dir="$base_dir\test-results"
    $package_dir = "$base_dir\deploy"
    $test_dir = "$out_dir\tests"
}

task default -depends local
task deploy-server -depends package-server, install-server
 

task package -depends package-server, package-client {    
    echo "Server and client packaged successfully. Bye bye."
}

task local -depends prepare, tokenize, test, package-client, package-server

task clean {
    #code
    rd $res_artefacts_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $res_artefacts_dir  -ErrorAction SilentlyContinue  | out-null
    
    #out dirs
    rd $out_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir "$out_dir\res" -ErrorAction SilentlyContinue  | out-null
    mkdir "$out_dir\res.client" -ErrorAction SilentlyContinue  | out-null
    mkdir "$test_dir\res.core.tests" -ErrorAction SilentlyContinue  | out-null
        
    #pkg dirs
    rd $package_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir "$package_dir" -ErrorAction SilentlyContinue  | out-null
}

task version -depends clean {
	 $commitHashAndTimestamp = Get-GitCommitHashAndTimestamp
     $commitHash = Get-GitCommitHash
     $timestamp = Get-GitTimestamp
     $branchName = Get-GitBranchOrTag
	 
	 $assemblyInfos = Get-ChildItem -Path $base_dir -Recurse -Filter AssemblyInfo.cs

	 $assemblyInfo = gc "$base_dir\AssemblyInfo.pson" | Out-String | iex
	 $version = $assemblyInfo.Version
	 #$productName = $assemblyInfo.ProductName
	 $companyName = $assemblyInfo.CompanyName
	 $copyright = $assemblyInfo.Copyright

	 try {
        foreach ($assemblyInfo in $assemblyInfos) {
            $path = Resolve-Path $assemblyInfo.FullName -Relative
            #Write-Host "Patching $path with product information."
            Patch-AssemblyInfo $path $Version $Version $branchName $commitHashAndTimestamp $companyName $copyright
        }         
    } catch {
        foreach ($assemblyInfo in $assemblyInfos) {
            $path = Resolve-Path $assemblyInfo.FullName -Relative
            Write-Host "Reverting $path to original state."
            & { git checkout --quiet $path }
        }
    }	
}

task compile -depends version {
	try{
		exec { msbuild $res_dir\Res.sln /t:Clean /t:Build /p:Configuration=$config /v:q /nologo }
	} finally{
		$assemblyInfos = Get-ChildItem -Path $base_dir -Recurse -Filter AssemblyInfo.cs
		foreach ($assemblyInfo in $assemblyInfos) {
            $path = Resolve-Path $assemblyInfo.FullName -Relative
            Write-Verbose "Reverting $path to original state."
            & { git checkout --quiet $path }
        }
	}
}

task prepare -depends compile {       
    exec {
        copy-item $res_artefacts_dir\* $out_dir\res\ 
    }
        
    exec {
        copy-item $resclient_artefacts_dir\* $out_dir\res.client\
    }
    
    exec {
        copy-item $res_test_dir\* $test_dir\res.core.tests\
    }
}

task tokenize -depends tokenize-server, tokenize-tests

task tokenize-server {
    $env_dir = "$base_dir\env\$env"
    
    exec {
        & "$tools_dir\config-transform\config-transform.exe" "$out_dir\res\res.exe.config" "$env_dir\res\App.$config.config"
    }

    if(test-path ("$env_dir\res\Nlog.config")) {
		       
        exec {
            copy-item "$env_dir\res\Nlog.config" "$out_dir\res\Nlog.config"
    	}   
    }
}

task tokenize-tests {
    $env_dir = "$base_dir\env\$env"
    
    exec {
        & "$tools_dir\config-transform\config-transform.exe" "$test_dir\res.core.tests\res.core.tests.dll.config" "$env_dir\res.tests\App.$config.config"
    }
}


task test {    
    $testassemblies = get-childitem "$test_dir\res.core.tests" -recurse -include *tests*.dll
    mkdir $test_results_dir  -ErrorAction SilentlyContinue  | out-null
    exec { 
        & $tools_dir\NUnit2.6.3\nunit-console-x86.exe $testassemblies /nologo /nodots /xml=$test_results_dir\res.core.tests_results.xml; 
    }
}

task package-server -depends tokenize-server {  
    mkdir "$package_dir\res" -ErrorAction SilentlyContinue  | out-null
    echo "Target: $package_dir\res\"
    
    exec {
        copy-item "$out_dir\res\*" "$package_dir\res\" -Exclude "logs"
    }
    
    exec {
        copy-item "$base_dir\env\$env\res\res-server-params.pson" "$package_dir\"
    }
}

task package-client -depends tokenize {
    mkdir "$package_dir\res.client" -ErrorAction SilentlyContinue  | out-null      
    echo "Target: $package_dir\res.client\"
    
    exec {
        copy-item "$out_dir\res.client\*" "$package_dir\res.client\"
    }

}

task nuget-client -depends build-client-nuget, publish-client-nuget

task build-client-nuget -depends compile {
	$commitHashAndTimestamp = Get-GitCommitHashAndTimestamp
    $commitHash = Get-GitCommitHash
    $timestamp = Get-GitTimestamp
    $branchName = Get-GitBranchOrTag
	
	$assemblyInfos = Get-ChildItem -Path $base_dir -Recurse -Filter AssemblyInfo.cs

	$assemblyInfo = gc "$base_dir\AssemblyInfo.pson" | Out-String | iex
	$version = $assemblyInfo.Version
	#$productName = $assemblyInfo.ProductName
	$companyName = $assemblyInfo.CompanyName
	$copyright = $assemblyInfo.Copyright

	try {
       foreach ($assemblyInfo in $assemblyInfos) {
           $path = Resolve-Path $assemblyInfo.FullName -Relative
           #Write-Host "Patching $path with product information."
           Patch-AssemblyInfo $path $Version $Version $branchName $commitHashAndTimestamp $companyName $copyright
       }         
    } catch {
        foreach ($assemblyInfo in $assemblyInfos) {
            $path = Resolve-Path $assemblyInfo.FullName -Relative
            Write-Host "Reverting $path to original state."
            & { git checkout --quiet $path }
        }
    }
	
	try{
		Push-Location "$res_dir\Res.Client"
		#exec { & "$res_dir\.nuget\NuGet.exe" "spec"}
		exec { & "$res_dir\.nuget\nuget.exe" pack Res.Client.csproj -IncludeReferencedProjects}
	} finally{
		Pop-Location
		$assemblyInfos = Get-ChildItem -Path $base_dir -Recurse -Filter AssemblyInfo.cs
		foreach ($assemblyInfo in $assemblyInfos) {
            $path = Resolve-Path $assemblyInfo.FullName -Relative
            #Write-Verbose "Reverting $path to original state."
            & { git checkout --quiet $path }
        }
	}	
}

task publish-client-nuget -depends build-client-nuget {
	$pkgPath = Get-ChildItem -Path "$res_dir\Res.Client" -Filter "*.nupkg" | select-object -first 1
	exec { & "$res_dir\.nuget\nuget.exe" push "$res_dir\Res.Client\$pkgPath" }
	ri "$res_dir\Res.Client\$pkgPath"
}

task install-server {
    echo "Initialising server installation."
    
    echo "Fetching parameters"
    $svcParams = gc "$package_dir\res-server-params.pson" | Out-String | iex
    
    echo "Parameters fetched. Checking for existing installation."
    
    $username = $svcParams.User
    $password = $svcParams.Password
    $serviceName = $svcParams.ServiceName
    $displayName = $svcParams.DisplayName
    $description = $svcParams.ServiceDescription  
    $reinstall = $svcParams.Reinstall
    
    $TargetFolder = $svcParams.TargetFolder
    $BackupFolder = $svcParams.BackupFolder
    
    $exePath = "$TargetFolder\res.exe"
    
    $existing = Get-Service $serviceName -ErrorAction SilentlyContinue
        
    if(test-path $exePath) {
        echo "$exePath found."
        
        if($existing){
            echo "Attempting to stop existing service $serviceName"
            
            exec {
                & "$exePath" stop -servicename:$serviceName
            }
            
            echo "Existing service stopped."
            
            if ($reinstall){
                echo "Uninstalling $serviceName"
                exec {
                    & "$exePath" uninstall -servicename:$serviceName
                }
                echo "Uninstalled $serviceName"
            }
        }
        else {
            echo "$serviceName is not installed as a service on this machine."
        }
        
        echo "Backing up target folder to $BackupFolder"
        
        
        rd "$BackupFolder" -recurse -force  -ErrorAction SilentlyContinue | out-null
        mkdir "$BackupFolder" -ErrorAction SilentlyContinue  | out-null 
        
        exec {
            copy-item "$TargetFolder\*" "$BackupFolder\"
        }
        
        echo "Target folder $TargetFolder backed up to $BackupFolder."
        echo "Deleting contents of target folder."
        
        ri -Recurse -Force "$TargetFolder" 
        
        echo "Target folder cleared."
    } else {
        echo "Existing service not found at $TargetFolder"
    }
    
    echo "Copying new files to $TargetFolder"
        
    mkdir "$TargetFolder" -ErrorAction SilentlyContinue  | out-null 
    
    exec {
            copy-item "$base_dir\deploy\res\*" "$TargetFolder\"
    }
    
    echo "New files copied to target folder."   
    
    ###############################################
    ##Files in target folder at this point.
    ############################################### 
    
    if(-not($existing) -or $reinstall){
    
        echo "Installing service"
                
        exec {
            & "$exePath" install -username:"$username" -password "`"$password`"" -servicename:$serviceName -description "`"$description`"" -displayname "`"$displayName`""
        }
        
        echo "Installed service"
    }
    
    echo "Starting service"
    exec {
        & "$exePath" start -servicename:$serviceName
    }
}